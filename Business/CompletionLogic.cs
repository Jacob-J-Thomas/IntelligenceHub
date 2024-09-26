using IntelligenceHub.API.MigratedDTOs;
using IntelligenceHub.API.MigratedDTOs.ToolDTOs;
using IntelligenceHub.Client;
using IntelligenceHub.Common;
using IntelligenceHub.Common.Exceptions;
using IntelligenceHub.DAL;
using IntelligenceHub.Host.Config;
using OpenAI.Chat;
using OpenAICustomFunctionCallingAPI.API.MigratedDTOs;
using System.Net;
using static IntelligenceHub.Common.GlobalVariables;

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
        private readonly VectorEmbeddingClient _embeddingClient;
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
            _embeddingClient = new VectorEmbeddingClient(settings.AIEndpoint, settings.AIKey);
            //_AIStreamingClient = new AIStreamingClient(settings.AIEndpoint, settings.AIKey);
            _functionClient = new FunctionClient(clientFactory);
            _profileDb = new ProfileRepository(settings.DbConnectionString);
            _messageHistoryRepository = new MessageHistoryRepository(settings.DbConnectionString);
            _ragRepository = new RagRepository(settings.RagDbConnectionString);
            _ragMetaRepository = new RagMetaRepository(settings.DbConnectionString);
        }

        #region Streaming
        public async IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest)
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

            var completionCollection = _AIClient.StreamCompletion(completionRequest);
            if (completionCollection == null) throw new IntelligenceHubException(500, "Something went wrong...");

            Role dbMessageRole;
            var allCompletionChunks = string.Empty;
            var toolCallDictionary = new Dictionary<string, string>();
            await foreach (var update in completionCollection)
            {
                if (update == null) continue;

                toolCallDictionary = update.ToolCalls;
                allCompletionChunks += update.CompletionUpdate;

                var roleString = update.Role.ToString();
                if (!string.IsNullOrEmpty(roleString))
                {
                    dbMessageRole = GlobalVariables.ConvertStringToRole(roleString);
                    update.Role = dbMessageRole;
                }
             
                yield return update;
            }

            if (toolCallDictionary.Any())
            {
                if (!string.IsNullOrEmpty(allCompletionChunks)) completionRequest.Messages.Add(new Message() { Content = allCompletionChunks, Role = Role.Assistant });
                var toolExecutionResponses = await ExecuteTools(toolCallDictionary, completionRequest.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId, streaming: true);
                yield return new CompletionStreamChunk()
                {
                    ToolExecutionResponses = toolExecutionResponses
                };
            }

            // save new messages to database
            if (completionRequest.ConversationId is Guid)
            {
                var dbMessage = DbMappingHandler.MapToDbMessage(new Message() { Role = Role.Assistant, Content = allCompletionChunks }, completionRequest.ConversationId, toolCallDictionary.Keys.ToArray());
                await _messageHistoryRepository.AddAsync(dbMessage);

                var dbUserMessage = DbMappingHandler.MapToDbMessage(new Message() { Role = Role.User, Content = completionRequest.Messages.Last(m => m.Role == Role.User).Content }, completionRequest.ConversationId, null);
                await _messageHistoryRepository.AddAsync(dbUserMessage);
            }
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

            if (completionRequest.ConversationId is Guid id)
            {
                var dbMessage = DbMappingHandler.MapToDbMessage(completion.Messages.Last(m => m.Role == Role.Assistant || m.Role == Role.Tool), completionRequest.ConversationId, completion.ToolCalls.Keys.ToArray());
                await _messageHistoryRepository.AddAsync(dbMessage);

                var dbUserMessage = DbMappingHandler.MapToDbMessage(completion.Messages.Last(m => m.Role == Role.User), completionRequest.ConversationId, null);
                await _messageHistoryRepository.AddAsync(dbUserMessage);
            }
            
            if (completion.FinishReason == FinishReason.ToolCalls)
            {
                var toolExecutionResponses = await ExecuteTools(completion.ToolCalls, completion.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId, streaming: false);
                completion.ToolExecutionResponses.AddRange(toolExecutionResponses);
            }
            return completion;
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

        public async Task<List<Tool>> BuildProfileReferenceTool(string[] profileNames)
        {
            var tools = new List<Tool>();
            foreach (var name in profileNames)
            {
                var profile = await _profileDb.GetByNameAsync(name);
                if (profile == null) return null;
                tools.Add(new ProfileReferenceTools(profile));
            }
            return tools;
        }

        // seperate this into two functions probably
        public async Task<List<HttpResponseMessage>> ExecuteTools(Dictionary<string, string> toolCalls, List<Message> messages, Profile? options = null, Guid? conversationId = null, bool streaming = false)// ChatCompletion
        {
            var modelRecursionTools = new Dictionary<string, string>();
            var functionResults = new List<HttpResponseMessage>();

            // Handle standard tool calls and collect recursive calls
            foreach (var tool in toolCalls)
            {
                var toolName = tool.Key.Replace("_Reference_AI_Model", "");
                if (tool.Key.Contains("_Reference_AI_Model"))
                {
                    modelRecursionTools.Add(toolName, tool.Value);
                }
                else
                {
                    var dbTool = await _toolDb.GetByNameAsync(toolName);
                    var toolExecutionUrl = dbTool.ExecutionUrl;
                    var toolExecutionMethod = dbTool.ExecutionMethod ?? HttpMethod.Post.ToString();

                    // how do we determine if the below is a post, get etc.?
                    if (!string.IsNullOrEmpty(toolExecutionUrl))
                    {
                        functionResults.Add(await _functionClient.CallFunction(tool.Key, tool.Value, toolExecutionUrl, toolExecutionMethod));
                    }
                }
            }

            // Handle recursive completions - i.e. have other models add a message before responding
            foreach (var tool in modelRecursionTools)
            {
                var completionRequest = new CompletionRequest()
                {
                    ConversationId = conversationId,
                    Profile = tool.Key,
                    Messages = messages,
                    ProfileOptions = options ?? new Profile()
                };

                // add anything the previous model wanted to forward
                if (!string.IsNullOrEmpty(tool.Value)) completionRequest.Messages.Add(new Message() { Role = Role.Assistant, Content = tool.Value });
                if (streaming)
                {
                    await foreach (var _ in StreamCompletion(completionRequest));
                }
                else await ProcessCompletion(completionRequest);
            }
            return functionResults;
        }
        #endregion
    }
}
