using Azure.AI.OpenAI;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.MessageDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs.SystemTools;
using OpenAICustomFunctionCallingAPI.Client;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.DAL;
using OpenAICustomFunctionCallingAPI.Host.Config;
using System.Net;

namespace OpenAICustomFunctionCallingAPI.Business
{
    public class CompletionLogic : ICompletionLogic
    {
        //private readonly IConfiguration _configuration;
        private readonly AIStreamingClient _AIStreamingClient;
        private readonly AIClient _AIClient;
        private readonly FunctionClient _FunctionClient;
        private readonly ProfileRepository _profileDb;
        private readonly ToolRepository _toolDb;
        private readonly ProfileToolsAssociativeRepository _profileToolAssocaitionDb;
        private readonly MessageHistoryRepository _messageHistoryRepository;
        private readonly List<HttpStatusCode> _serverSideErrorCodes;

        public CompletionLogic(Settings settings) 
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _profileToolAssocaitionDb = new ProfileToolsAssociativeRepository(settings.DbConnectionString);
            _toolDb = new ToolRepository(settings.DbConnectionString);
            _AIClient = new AIClient(settings.OpenAIEndpoint, settings.OpenAIKey);
            _AIStreamingClient = new AIStreamingClient(settings.OpenAIEndpoint, settings.OpenAIKey);
            _FunctionClient = new FunctionClient("https://Unimplemented.FunctionCallingService.NotARealWebsite.1234567890.com");
            _profileDb = new ProfileRepository(settings.DbConnectionString);
            _messageHistoryRepository = new MessageHistoryRepository(settings.DbConnectionString);
            _serverSideErrorCodes = new List<HttpStatusCode>() 
            { 
                HttpStatusCode.BadGateway,
                HttpStatusCode.GatewayTimeout,
                HttpStatusCode.InsufficientStorage,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.ServiceUnavailable,
            };
        }

        #region Streaming
        public async Task<StreamingResponse<StreamingChatCompletionsUpdate>> StreamCompletion(ChatRequestDTO completionRequest)
        {
            completionRequest.ConversationId = completionRequest.ConversationId ?? Guid.NewGuid();
            completionRequest.Modifiers = completionRequest.Modifiers ?? new BaseCompletionDTO();
            var responseDTO = new ChatResponseDTO();

            await _messageHistoryRepository.AddAsync(new DbMessageDTO(completionRequest)); // run this without async to improve speed

            var aiClientDTO = await BuildOpenAICompletion(completionRequest); // chose which AIClient to build and execute with here
            if (aiClientDTO == null)
            {
                return null;// adjust this to return 404s
            }

            aiClientDTO.Stream = true;
            return await _AIStreamingClient.StreamCompletion(aiClientDTO);
        }

        public string GetStreamAuthor(StreamingChatCompletionsUpdate chunk, ChatRequestDTO chatDTO)
        {
            var author = chunk.AuthorName;
            if (chunk.Role == "assistant")
            {
                author = chatDTO.ProfileName;
            }
            else if (chunk.Role == "user")
            {
                author = chatDTO.Modifiers.User ?? "user";
            }
            else if (chunk.Role == "tool")
            {
                author = "tool";
            }
            return author;
        }
        #endregion

        #region Controller
        public async Task<ChatResponseDTO> ProcessCompletion(ChatRequestDTO completionRequest)
        {
            // recursive completions contain all their chat history in the database
            completionRequest.ConversationId = completionRequest.ConversationId ?? Guid.NewGuid();
            var responseDTO = new ChatResponseDTO();

            await _messageHistoryRepository.AddAsync(new DbMessageDTO(completionRequest));
            
            // Build request DTO
            var aiClientDTO = await BuildOpenAICompletion(completionRequest); // chose which AIClient to build and execute with here
            if (aiClientDTO == null)
            {
                return null;// adjust this to return 404s
            }
            var completionResponse = await GetCompletion(completionRequest.ProfileName, aiClientDTO, attempts: 0);

            // if tool calls != null && content == null return "Please give me a moment to process this request."

            var defaultChoice = completionResponse.Choices.FirstOrDefault(); // this needs to be modified if we wish to select from multiple results at once later
            if (defaultChoice == null) 
            {
                return null;
            }

            // move this logic to the controller level like when streaming?
            responseDTO.ToolResponses = await ProcessCompletionResponse(defaultChoice, completionRequest.ProfileName, (Guid)completionRequest.ConversationId);
            responseDTO.Completion = defaultChoice.Message.Content ?? "Please hold on for a moment while I process your request...";
            responseDTO.ConversationId = (Guid)completionRequest.ConversationId;

            return responseDTO;
        }

        public async Task<List<HttpResponseMessage>> ProcessCompletionResponse(ResponseChoiceDTO response, string profileName, Guid conversationId)
        {
            // Add response to database conversation history
            if (response.Message.Content != null)
            {
                var responseMessage = new DbMessageDTO();
                responseMessage.ConvertToDbMessageDTO(conversationId, "assistant", profileName, response.Message.Content);
                await _messageHistoryRepository.AddAsync(responseMessage);
            }

            // process and return completion data
            if (response.Finish_Reason == "tool_calls")
            {
                var completionToolCalls = response.Message.Tool_Calls; 
                return await ExecuteTools(conversationId, response.Message.Tool_Calls, streaming: false);
            }
            return null;
        }

        // move to an AIClient in API layer
        public async Task<CompletionResponseDTO> GetCompletion(string profileName, DefaultCompletionDTO openAIRequest, int attempts)
        {
            var maxAttempts = 5;
            while (attempts < maxAttempts)
            {
                try
                {
                    var completionResponse = await _AIClient.PostCompletion(openAIRequest);

                    // improve this validation
                    if (completionResponse == null || completionResponse.Choices.FirstOrDefault() == null)
                    {
                        return null;
                    }
                    return completionResponse;
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode != null && _serverSideErrorCodes.Contains((HttpStatusCode)ex.StatusCode))
                    {
                        
                        // add logic to switch to backup services

                        await GetCompletion(profileName, openAIRequest, attempts++);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            throw new Exception("Completion request failed to generate function call after 3 attempts for a request requiring a call.");
        }
        #endregion

        #region Shared
        public async Task<DefaultCompletionDTO> BuildOpenAICompletion(ChatRequestDTO chatRequest)
        {
            // probably move this to a seperate method
            var completionProfile = await _profileDb.GetByNameWithToolsAsync(chatRequest.ProfileName);
            if (completionProfile == null)
            {
                var profileWithoutTools = await _profileDb.GetByNameAsync(chatRequest.ProfileName);
                if (profileWithoutTools == null)
                {
                    return null;
                }
                completionProfile = new APIProfileDTO(profileWithoutTools);
            }
            else
            {
                var profileToolDTOs = await _profileToolAssocaitionDb.GetToolAssociationsAsync(completionProfile.Id);
                completionProfile.Tools = new List<ToolDTO>();
                foreach (var association in profileToolDTOs)
                {
                    var tool = await _toolDb.GetToolByIdAsync(association.ToolID);
                    completionProfile.Tools.Add(tool);
                }
            }
            
            DefaultCompletionDTO openAIRequest;
            if (chatRequest.Modifiers != null)
            {
                openAIRequest = new DefaultCompletionDTO(completionProfile, chatRequest.Modifiers);
            }
            else
            {
                openAIRequest = new DefaultCompletionDTO(completionProfile);
            }

            if (completionProfile.Reference_Profiles != null && completionProfile.Reference_Profiles.Length > 0)
            {
                foreach (var profile in completionProfile.Reference_Profiles)
                {
                    openAIRequest.Tools.Add(await BuildProfileReferenceTool(profile));
                }
            }
            openAIRequest.Messages = await BuildMessages(chatRequest.Completion, openAIRequest.System_Message, chatRequest.ConversationId);
            return openAIRequest;
        }

        public async Task<ToolDTO> BuildProfileReferenceTool(string profileName)
        {
            var profile = await _profileDb.GetByNameAsync(profileName);
            if (profile == null)
            {
                return null;
            }
            var toolDto = new ProfileReferenceTools(profile);
            return toolDto;
        }

        public async Task<List<MessageDTO>> BuildMessages(string completion, string? systemMessage, Guid? conversationId)
        {
            var messages = new List<MessageDTO>();
            if (systemMessage != null)
            {
                messages.Add(new MessageDTO("system", systemMessage));
            }
            if (conversationId != null)
            {
                var dbMessages = await _messageHistoryRepository.GetConversationAsync((Guid)conversationId);
                foreach (var dbMessage in dbMessages)
                {
                    var message = new MessageDTO()
                    {
                        Role = dbMessage.Role,
                        Content = dbMessage.Content,
                    };
                    messages.Add(message);
                }
            }
            if (completion != null)
            {
                messages.Add(new MessageDTO("user", completion));
            }
            return messages;
        }

        // seperate this into two functions probably
        public async Task<List<HttpResponseMessage>> ExecuteTools(Guid? conversationId, List<ResponseToolDTO> tools, bool streaming)
        {
            var recursionTools = new List<ResponseToolDTO>();
            var functionResults = new List<HttpResponseMessage>();
            foreach (var tool in tools)
            {
                if (tool.Function.Name.Contains("Reference_AI_Model"))
                {
                    tool.Function.Name = tool.Function.Name.Replace("_Reference_AI_Model", "");
                    recursionTools.Add(tool);
                }
                else
                {
                    var result = await _FunctionClient.CallFunction(tool);
                    if (result != null)
                    {
                        functionResults.Add(result);
                    }
                }
            }

            foreach (var tool in recursionTools)
            {
                var completionRequest = new ChatRequestDTO()
                {
                    ConversationId = conversationId,
                    ProfileName = tool.Function.Name
                };
                if (streaming)
                {
                    await StreamCompletion(completionRequest);
                }
                else
                {
                    await ProcessCompletion(completionRequest);
                }
            }

            // prevent recursion call overflow somehow... Maybe add a column to profiles for max recursion?
            // - This would work strangely for conversations with models that use varying lengths
            return functionResults;
        }
        #endregion
    }
}
