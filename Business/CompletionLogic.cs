using Azure;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.AICompletionDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs.Response;
using IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs.SystemTools;
using IntelligenceHub.API.MigratedDTOs;
using IntelligenceHub.API.MigratedDTOs.ToolDTOs;
using IntelligenceHub.Client;
using IntelligenceHub.Common.Exceptions;
using IntelligenceHub.Controllers.DTOs;
using IntelligenceHub.DAL;
using IntelligenceHub.Host.Config;
using OpenAI.Chat;
using OpenAICustomFunctionCallingAPI.API.MigratedDTOs;
using OpenAICustomFunctionCallingAPI.DAL;
using System.Net;

namespace IntelligenceHub.Business
{
    public class CompletionLogic : ICompletionLogic
    {
        // move to GlobalVariables class
        private const int _defaultMessageHistory = 5;

        //private readonly IConfiguration _configuration;
        //private readonly AIStreamingClient _AIStreamingClient;
        private readonly AGIClient _AIClient;
        private readonly FunctionClient _functionClient;
        private readonly EmbeddingClient _embeddingClient;
        private readonly ProfileRepository _profileDb;
        private readonly ToolRepository _toolDb;
        private readonly ProfileToolsAssociativeRepository _profileToolAssocaitionDb;
        private readonly MessageHistoryRepository _messageHistoryRepository;
        private readonly RagRepository _ragRepository;
        private readonly RagMetaRepository _ragMetaRepository;
        private readonly List<HttpStatusCode> _serverSideErrorCodes = new List<HttpStatusCode>()
            {
                HttpStatusCode.BadGateway,
                HttpStatusCode.GatewayTimeout,
                HttpStatusCode.InsufficientStorage,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.ServiceUnavailable,
            };

        public CompletionLogic(IHttpClientFactory clientFactory, Settings settings) 
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _profileToolAssocaitionDb = new ProfileToolsAssociativeRepository(settings.DbConnectionString);
            _toolDb = new ToolRepository(settings.DbConnectionString);
            _AIClient = new AGIClient(settings.AIEndpoint, settings.AIKey);
            _embeddingClient = new EmbeddingClient(settings.AIEndpoint, settings.AIKey);
            //_AIStreamingClient = new AIStreamingClient(settings.AIEndpoint, settings.AIKey);
            _functionClient = new FunctionClient(clientFactory, "https://Unimplemented.FunctionCallingService.NotARealWebsite.1234567890.com");
            _profileDb = new ProfileRepository(settings.DbConnectionString);
            _messageHistoryRepository = new MessageHistoryRepository(settings.DbConnectionString);
            _ragRepository = new RagRepository(settings.RagDbConnectionString);
            _ragMetaRepository = new RagMetaRepository(settings.DbConnectionString);
        }

        #region Streaming
        public async Task<StreamingResponse<StreamingChatCompletionsUpdate>> StreamCompletion(CompletionRequest completionRequest)
        {
            if (completionRequest.RagData is not null)
            {
                completionRequest.Completion = await BuildRagMessage(completionRequest.RagData.RagDatabase, completionRequest.RagData.RagTarget, completionRequest.Completion, completionRequest.RagData.MaxRagDocs);
                if (completionRequest.Completion is null) return null;
            }
            completionRequest.ProfileModifiers = completionRequest.ProfileModifiers ?? new BaseCompletionDTO();
            if (completionRequest.ConversationId is not null) await _messageHistoryRepository.AddAsync(new DbMessageDTO(completionRequest)); // run this without async to improve speed
            var aiClientDTO = await BuildCompletion(completionRequest.ProfileName, completionRequest.Completion, completionRequest.ProfileModifiers, completionRequest.ConversationId, completionRequest.MaxMessageHistory);
            if (aiClientDTO == null) return null;// adjust this to return 404s
            aiClientDTO.Stream = true;

            ensure messages are handled properly here

            return await _AIClient.StreamCompletion(aiClientDTO);
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
        public async Task<CompletionResponse> ProcessCompletion(CompletionRequest completionRequest)
        {
            // Get and set profile details, overriding the database with any parameters that aren't null in the request
            var profile = await _profileDb.GetByNameWithToolsAsync(completionRequest.Profile);
            if (profile == null) throw new IntelligenceHubException(404, $"The profile '{completionRequest.Profile}' does not exist.");
            completionRequest.ProfileOptions = await BuildCompletionOptions(profile, completionRequest.ProfileOptions);

            // Get and attach message history if a conversation id exists
            if (completionRequest.ConversationId is Guid conversationId)
            {
                completionRequest.Messages = await BuildAndUpdateMessageHistory(
                    conversationId,
                    completionRequest.Messages,
                    completionRequest.ProfileOptions.MaxMessageHistory);
            }

            var completion = await _AIClient.PostCompletion(completionRequest);
            if (completion == null) throw new IntelligenceHubException(500, "Something went wrong...");

            // build the response object
            var responseMessage = new Message()
            {
                Content = completion.Content.ToString(),
                Role = completion.Role.ToString()
            };

            var response = new CompletionResponse()
            {
                FinishReason = completion.FinishReason.ToString(),
                Messages = completionRequest.Messages
            };

            // attach the completion message to the return list
            response.Messages.Add(responseMessage);

            if (completionRequest.ConversationId is Guid id)
            {
                var toolsCalled = completion.ToolCalls.Select(x => x.FunctionName).ToArray();
                var dbMessage = DbMappingHandler.MapToDbMessage(responseMessage, completionRequest.ConversationId, toolsCalled);
                await _messageHistoryRepository.AddAsync(dbMessage);
            }
            
            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                var toolExecutionResponses = await ExecuteTools(completionRequest.ConversationId, completion, streaming: false);
                response.ToolExecutionResponses.AddRange(toolExecutionResponses);
            }
            return response;
        }

        public async Task<List<Message>> BuildAndUpdateMessageHistory(Guid conversationId, List<Message> requestMessages, int? maxMessageHistory = null)
        {
            var allMessages = new List<Message>();
            var messageHistory = await _messageHistoryRepository.GetConversationAsync(conversationId, maxMessageHistory ?? _defaultMessageHistory);
            if (messageHistory == null) throw new IntelligenceHubException(404, $"A conversation with id '{conversationId}' does not exist");

            // add messages to the database
            foreach (var message in messageHistory)
            {
                // Move to DAL layer
                var dbMessage = DbMappingHandler.MapToDbMessage(message, conversationId);
                await _messageHistoryRepository.AddAsync(dbMessage);
            }

            // ensure the messages are properly arranged, with the user completion appearing very last
            allMessages.AddRange(messageHistory);
            allMessages.AddRange(requestMessages);

            // Reduce the amount of messages to reflect the MaxMessageHistory property
            if (maxMessageHistory is int maxMessages) allMessages.RemoveRange(maxMessages, allMessages.Count - maxMessages);
            return allMessages;
        }

        public async Task<Profile> BuildCompletionOptions(Profile profile, Profile profileOptions)
        {
            // adds the profile references to the tools
            if (profileOptions.Tools == null) profileOptions.Tools = new List<Tool>();
            if (profileOptions.Reference_Profiles != null) profileOptions.Tools.AddRange(await BuildProfileReferenceTool(profileOptions.Reference_Profiles));

            return new Profile()
            {
                Id = profile.Id,
                Name = profile.Name,
                Model = profileOptions.Model ?? profile.Model,
                RagDatabase = profileOptions.RagDatabase ?? profile.RagDatabase,
                MaxMessageHistory = profileOptions.MaxMessageHistory ?? profile.MaxMessageHistory,
                Max_Tokens = profileOptions.Max_Tokens ?? profile.Max_Tokens,
                Temperature = profileOptions.Temperature ?? profile.Temperature,
                Top_P = profileOptions.Top_P ?? profile.Top_P,
                Frequency_Penalty = profileOptions.Frequency_Penalty ?? profile.Frequency_Penalty,
                Presence_Penalty = profileOptions.Presence_Penalty ?? profile.Presence_Penalty,
                Seed = profileOptions.Seed ?? profile.Seed,
                Stop = profileOptions.Stop ?? profile.Stop,
                Logprobs = profileOptions.Logprobs ?? profile.Logprobs,
                Top_Logprobs = profileOptions.Top_Logprobs ?? profile.Top_Logprobs,
                Response_Format = profileOptions.Response_Format ?? profile.Response_Format,
                User = profileOptions.User ?? profile.User,
                Tools = profileOptions.Tools ?? profile.Tools,
                Tool_Choice = profileOptions.Tool_Choice ?? profile.Tool_Choice,
                System_Message = profileOptions.System_Message ?? profile.System_Message,
                Return_Recursion = profileOptions.Return_Recursion ?? profile.Return_Recursion,
                Reference_Profiles = profileOptions.Reference_Profiles ?? profile.Reference_Profiles,
            };
        }
        #endregion

        #region Shared
        //public async Task<ChatCompletion> BuildCompletion(string profileName, string? completion = null, BaseCompletionDTO? modifiers = null, Guid? conversationId = null, int? maxMessageHistory = null)
        //{
        //    // probably move this to a seperate method
        //    var completionProfile = await _profileDb.GetByNameWithToolsAsync(profileName);
        //    if (completionProfile is null)
        //    {
        //        var profileWithoutTools = await _profileDb.GetByNameAsync(profileName);
        //        if (profileWithoutTools is null) return null;
        //        completionProfile = new Profile(profileWithoutTools);
        //    }
        //    else
        //    {
        //        var profileToolDTOs = await _profileToolAssocaitionDb.GetToolAssociationsAsync(completionProfile.Id);
        //        completionProfile.Tools = new List<ToolDTO>();
        //        foreach (var association in profileToolDTOs) completionProfile.Tools.Add(await _toolDb.GetToolByIdAsync(association.ToolID));
        //    }

        //    var openAIRequest = new ChatCompletion()
        //    {
        //        Content = new UserChatMessage(completion),
        //        d
        //    };

        //    if (modifiers is not null) openAIRequest = new DefaultCompletionDTO(completionProfile, modifiers);
        //    else openAIRequest = new DefaultCompletionDTO(completionProfile);


        //    if (completionProfile.Reference_Profiles is not null && completionProfile.Reference_Profiles.Length > 0)
        //        foreach (var profile in completionProfile.Reference_Profiles) openAIRequest.Tools.Add(await BuildProfileReferenceTool(profile));
        //    if (completion is not null) openAIRequest.Messages = await BuildMessages(completion, openAIRequest.System_Message, conversationId, maxMessageHistory);
        //    else if (modifiers is ClientBasedCompletion clientBasedCompletion) openAIRequest.Messages = clientBasedCompletion.Messages;
        //    return openAIRequest;
        //}

        public async Task<List<Tool>> BuildProfileReferenceTool(string[] profileNames)
        {
            var tools = new List<Tool>();
            foreach (var name in profileNames)
            {
                var profile = await _profileDb.GetByNameAsync(name);
                if (profile == null) return null;
                tools.Add(new ProfileReferenceTools(profile)); change this
            }
            return tools;
        }

        //public async Task<List<ChatMessage>> BuildMessages(string completion, string? systemMessage, Guid? conversationId, int? maxMessageHistory)
        //{
        //    var messages = new List<ChatMessage>();
        //    if (systemMessage != null) messages.Add(new SystemChatMessage(systemMessage));
        //    if (conversationId != null && maxMessageHistory > 0)
        //    {
        //        var completionMessageCount = 1; // this will always = 1
        //        var maxDbMessagesToRetrieve = maxMessageHistory ?? 5 - completionMessageCount;
        //        var dbMessages = new List<DbMessage>();
        //        if (conversationId is not null) dbMessages = await _messageHistoryRepository.GetConversationAsync((Guid)conversationId, maxDbMessagesToRetrieve);
        //        foreach (var dbMessage in dbMessages)
        //        {
        //            if (dbMessage.Role == ChatMessageRole.User.ToString()) messages.Add(new UserChatMessage(dbMessage.Content));
        //            else if (dbMessage.Role == ChatMessageRole.Assistant.ToString()) messages.Add(new AssistantChatMessage(dbMessage.Content));
        //            else if (dbMessage.Role == ChatMessageRole.Tool.ToString()) messages.Add(new ToolChatMessage(dbMessage.Content));
        //        }
        //    }
        //    if (completion != null) messages.Add(new UserChatMessage(completion));
        //    return messages;
        //}

        // seperate this into two functions probably
        public async Task<List<HttpResponseMessage>> ExecuteTools(Guid? conversationId, ChatCompletion toolCompletion, bool streaming = false)
        {
            var recursionTools = new List<ResponseToolDTO>();
            var functionResults = new List<HttpResponseMessage>();

            // Handle recursive completions - i.e. have other models add a message before responding
            foreach (var tool in toolCompletion.ToolCalls)
            {
                if (tool.FunctionName.Contains("_Reference_AI_Model"))
                {
                    tool.FunctionName = tool.FunctionName.Replace("_Reference_AI_Model", "");
                    recursionTools.Add(tool);
                }
                else
                {
                    var result = await _functionClient.CallFunction(tool);
                    if (result != null) functionResults.Add(result);
                }
            }

            // Handle standard tool calls
            foreach (var tool in recursionTools)
            {
                var completionRequest = new ChatCompletion()
                {
                    ConversationId = conversationId,
                    ProfileName = tool.Function.Name
                };
                if (streaming) await StreamCompletion(completionRequest);
                else await ProcessCompletion(completionRequest);
            }
            // prevent recursion call overflow somehow... Maybe add a column to profiles for max recursion?
            // - This would work strangely for conversations with models that use varying lengths
            // - alternatively this value could be supplied by the client, and a default value would be used otherwise
            return functionResults;
        }
        #endregion
    }
}
