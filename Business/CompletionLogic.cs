using Azure.AI.OpenAI;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.EmbeddingDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.MessageDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs.SystemTools;
using OpenAICustomFunctionCallingAPI.Client;
using OpenAICustomFunctionCallingAPI.Common.Extensions;
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
        private readonly AGIClient _AIClient;
        private readonly FunctionClient _functionClient;
        private readonly EmbeddingClient _embeddingClient;
        private readonly ProfileRepository _profileDb;
        private readonly ToolRepository _toolDb;
        private readonly ProfileToolsAssociativeRepository _profileToolAssocaitionDb;
        private readonly MessageHistoryRepository _messageHistoryRepository;
        private readonly RagRepository _ragRepository;
        private readonly RagMetaRepository _ragMetaRepository;
        private readonly List<HttpStatusCode> _serverSideErrorCodes;

        public CompletionLogic(Settings settings) 
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _profileToolAssocaitionDb = new ProfileToolsAssociativeRepository(settings.DbConnectionString);
            _toolDb = new ToolRepository(settings.DbConnectionString);
            _AIClient = new AGIClient(settings.AIEndpoint, settings.AIKey);
            _embeddingClient = new EmbeddingClient(settings.AIEndpoint, settings.AIKey);
            _AIStreamingClient = new AIStreamingClient(settings.AIEndpoint, settings.AIKey);
            _functionClient = new FunctionClient("https://Unimplemented.FunctionCallingService.NotARealWebsite.1234567890.com");
            _profileDb = new ProfileRepository(settings.DbConnectionString);
            _messageHistoryRepository = new MessageHistoryRepository(settings.DbConnectionString);
            _ragRepository = new RagRepository(settings.RagDbConnectionString);
            _ragMetaRepository = new RagMetaRepository(settings.DbConnectionString);
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
            completionRequest.ProfileModifiers = completionRequest.ProfileModifiers ?? new BaseCompletionDTO();
            if (completionRequest.ConversationId is not null) await _messageHistoryRepository.AddAsync(new DbMessageDTO(completionRequest)); // run this without async to improve speed
            var aiClientDTO = await BuildCompletion(completionRequest.ProfileName, completionRequest.Completion, completionRequest.ProfileModifiers, completionRequest.ConversationId, completionRequest.MaxMessageHistory);
            if (aiClientDTO == null) return null;// adjust this to return 404s
            aiClientDTO.Stream = true;
            return await _AIStreamingClient.StreamCompletion(aiClientDTO);
        }

        public async Task<StreamingResponse<StreamingChatCompletionsUpdate>> StreamClientBasedCompletion(ClientBasedCompletion completionRequest)
        {
            var aiClientDTO = await BuildCompletion(completionRequest.ProfileName, null, completionRequest);
            if (aiClientDTO == null) return null;// adjust this to return 404s
            aiClientDTO.Stream = true;
            return await _AIStreamingClient.StreamCompletion(aiClientDTO);
        }

        public string GetStreamAuthor(StreamingChatCompletionsUpdate chunk, string profileName, string user = "user")
        {
            var author = chunk.AuthorName;
            if (chunk.Role == "assistant") author = profileName;
            else if (chunk.Role == "user") author = user;
            else if (chunk.Role == "tool") author = "tool";
            return author;
        }
        #endregion

        #region Controller
        public async Task<ChatResponseDTO> ProcessCompletion(ChatRequestDTO completionRequest)
        {
            // recursive completions contain all their chat history in the database
            //completionRequest.ConversationId = completionRequest.ConversationId ?? Guid.NewGuid();
            var responseDTO = new ChatResponseDTO();

            if (completionRequest.RagData is not null)
            {
                completionRequest.Completion = await BuildRagMessage(completionRequest.RagData.RagDatabase, completionRequest.RagData.RagTarget, completionRequest.Completion, completionRequest.RagData.MaxRagDocs);
                if (completionRequest.Completion == null) return null;
            }

            // Build request DTO
            var aiClientDTO = await BuildCompletion(completionRequest.ProfileName, completionRequest.Completion, completionRequest.ProfileModifiers, completionRequest.ConversationId, completionRequest.MaxMessageHistory); // chose which AIClient to build and execute with here
            if (aiClientDTO is null) return null;// adjust this to return 404s
            if (completionRequest.ConversationId is not null) await _messageHistoryRepository.AddAsync(new DbMessageDTO(completionRequest));
            var completionResponse = await GetCompletion(aiClientDTO, attempts: 0);
            var defaultChoice = completionResponse.Choices.FirstOrDefault(); // this needs to be modified if we wish to select from multiple results at once later
            if (defaultChoice is null) return null;

            // move this logic to the controller level like when streaming?
            responseDTO.ConversationId = completionRequest.ConversationId ?? null;
            responseDTO.ToolResponses = await ProcessCompletionResponse(defaultChoice, completionRequest.ProfileName, responseDTO.ConversationId);
            responseDTO.Completion = defaultChoice.Message.Content ?? "Please hold on for a moment while I process your request...";
            return responseDTO;
        }

        public async Task<ChatResponseDTO> ProcessClientBasedCompletion(ClientBasedCompletion completionRequest)
        {
            // recursive completions contain all their chat history in the database
            var responseDTO = new ChatResponseDTO();

            if (completionRequest.RagData is not null)
            {
                completionRequest.Messages[0].Content = await BuildRagMessage(completionRequest.RagData.RagDatabase, completionRequest.RagData.RagTarget, completionRequest.Messages[0].Content, completionRequest.RagData.MaxRagDocs);
                if (completionRequest.Messages[0].Content == null) return null;
            }

            // Build request DTO
            var aiClientDTO = await BuildCompletion(completionRequest.ProfileName, null, completionRequest);
            if (aiClientDTO is null) return null;// adjust this to return 404s
            var completionResponse = await GetCompletion(aiClientDTO, attempts: 0);
            var defaultChoice = completionResponse.Choices.FirstOrDefault(); // this needs to be modified if we wish to select from multiple results at once later
            if (defaultChoice is null) return null;

            // move this logic to the controller level like when streaming?
            responseDTO.ToolResponses = await ProcessCompletionResponse(defaultChoice, completionRequest.Model, null);
            responseDTO.Completion = defaultChoice.Message.Content ?? "Please hold on for a moment while I process your request...";
            return responseDTO;
        }

        public async Task<string> BuildRagMessage(string database, string target, string completion, int? maxRagDocs)
        {
            var ragMetadata = await _ragMetaRepository.GetByNameAsync(database);
            if (ragMetadata is null || maxRagDocs is null || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(completion)) return null;
            var ragInstructionData = "Below you will find a set of documents, each delimited with tripple backticks. " +
                "Please use these documents to inform your response to the dialogue below the documents.\n\n";

            _ragRepository.SetTable(database);
            EmbeddingRequestBase embeddingRequest = new()
            {
                Input = completion,
                Model = ragMetadata.VectorModel,
                Encoding_Format = ragMetadata.EncodingFormat,
                Dimensions = ragMetadata.Dimensions
            };
            var embedding = await _embeddingClient.GetEmbeddings(embeddingRequest);
            var embeddingBinary = embedding.Data[0].Embedding.EncodeToBinary();
            var documents = await _ragRepository.CosineSimilarityQueryAsync(target, embeddingBinary, (int)maxRagDocs);
            foreach (var doc in documents) ragInstructionData += 
                    "\n```\n" + 
                    $"Title: {doc.Title}\n" +
                    $"SourceName: {doc.SourceName}\n" +
                    $"SourceLink: {doc.SourceLink}\n" +
                    $"SourceName: {doc.SourceName}\n" +
                    $"Content: {doc.Content}" + 
                    "\n```\n";

            return ragInstructionData + "\n\n" + completion;
        }

        public async Task<List<HttpResponseMessage>> ProcessCompletionResponse(ResponseChoiceDTO response, string profileName, Guid? conversationId)
        {
            // Add response to database conversation history
            if (response.Message.Content is not null)
            {
                var responseMessage = new DbMessageDTO();
                responseMessage.ConvertToDbMessageDTO(conversationId, "assistant", profileName, response.Message.Content);
                if (conversationId is not null) await _messageHistoryRepository.AddAsync(responseMessage);
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
        public async Task<CompletionResponseDTO> GetCompletion(DefaultCompletionDTO openAIRequest, int attempts)
        {
            var maxAttempts = 5;
            while (attempts < maxAttempts)
            {
                try
                {
                    var completionResponse = await _AIClient.PostCompletion(openAIRequest);

                    // improve this validation
                    if (completionResponse is null || completionResponse.Choices.FirstOrDefault() is null)
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

                        await GetCompletion(openAIRequest, attempts++);
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
        public async Task<DefaultCompletionDTO> BuildCompletion(string profileName, string? completion = null, BaseCompletionDTO? modifiers = null, Guid? conversationId = null, int? maxMessageHistory = null)
        {
            // probably move this to a seperate method
            var completionProfile = await _profileDb.GetByNameWithToolsAsync(profileName);
            if (completionProfile is null)
            {
                var profileWithoutTools = await _profileDb.GetByNameAsync(profileName);
                if (profileWithoutTools is null) return null;
                completionProfile = new APIProfileDTO(profileWithoutTools);
            }
            else
            {
                var profileToolDTOs = await _profileToolAssocaitionDb.GetToolAssociationsAsync(completionProfile.Id);
                completionProfile.Tools = new List<ToolDTO>();
                foreach (var association in profileToolDTOs) completionProfile.Tools.Add(await _toolDb.GetToolByIdAsync(association.ToolID));
            }
            
            DefaultCompletionDTO openAIRequest;
            if (modifiers is not null) openAIRequest = new DefaultCompletionDTO(completionProfile, modifiers);
            else openAIRequest = new DefaultCompletionDTO(completionProfile);
            if (completionProfile.Reference_Profiles is not null && completionProfile.Reference_Profiles.Length > 0)
                foreach (var profile in completionProfile.Reference_Profiles) openAIRequest.Tools.Add(await BuildProfileReferenceTool(profile));
            if (completion is not null) openAIRequest.Messages = await BuildMessages(completion, openAIRequest.System_Message, conversationId, maxMessageHistory);
            else if (modifiers is ClientBasedCompletion clientBasedCompletion) openAIRequest.Messages = clientBasedCompletion.Messages;
            return openAIRequest;
        }

        public async Task<ToolDTO> BuildProfileReferenceTool(string profileName)
        {
            var profile = await _profileDb.GetByNameAsync(profileName);
            if (profile == null) return null;
            return new ProfileReferenceTools(profile);
        }

        public async Task<List<MessageDTO>> BuildMessages(string completion, string? systemMessage, Guid? conversationId, int? maxMessageHistory)
        {
            var messages = new List<MessageDTO>();
            if (systemMessage != null) messages.Add(new MessageDTO("system", systemMessage));
            if (conversationId != null && maxMessageHistory > 0)
            {
                var completionMessageCount = 1; // this will always = 1
                var maxDbMessagesToRetrieve = maxMessageHistory ?? 5 - completionMessageCount;
                var dbMessages = new List<DbMessageDTO>();
                if (conversationId is not null) dbMessages = await _messageHistoryRepository.GetConversationAsync((Guid)conversationId, maxDbMessagesToRetrieve);
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
            if (completion != null) messages.Add(new MessageDTO("user", completion));
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
                    var result = await _functionClient.CallFunction(tool);
                    if (result != null) functionResults.Add(result);
                }
            }
            foreach (var tool in recursionTools)
            {
                var completionRequest = new ChatRequestDTO()
                {
                    ConversationId = conversationId,
                    ProfileName = tool.Function.Name
                };
                if (streaming) await StreamCompletion(completionRequest);
                else await ProcessCompletion(completionRequest);
            }
            // prevent recursion call overflow somehow... Maybe add a column to profiles for max recursion?
            // - This would work strangely for conversations with models that use varying lengths
            return functionResults;
        }
        #endregion
    }
}
