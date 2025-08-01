﻿using Azure.Search.Documents.Models;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Business.Factories;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Interfaces;
using System.Text;
using System.Text.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Business logic for processing requests to the completion controller.
    /// </summary>
    public class CompletionLogic : ICompletionLogic
    {
        // move to GlobalVariables class
        private const int _defaultMessageHistory = 5;
        private const int _defaultMaxRecursionMessageHistory = 20;
        private const string _defaultUser = "User";

        private readonly IAGIClientFactory _agiClientFactory;
        private readonly IRagClientFactory _ragClientFactory;
        private readonly IToolClient _ToolClient;
        private readonly IProfileRepository _profileDb;
        private readonly IToolRepository _toolDb;
        private readonly IMessageHistoryRepository _messageHistoryRepository;
        private readonly IIndexMetaRepository _ragMetaRepository;

        /// <summary>
        /// A constructor utilized to resolve dependencies for the completion logic via dependency injection.
        /// </summary>
        /// <param name="agiClientFactory">Client factory used to retrieve the client associated with request's host parameter.</param>
        /// <param name="searchClient">Search service client used for requests requiring RAG retrieval.</param>
        /// <param name="toolClient">HttpClient that can be used to send requests to tools that have an associated endpoint.</param>
        /// <param name="toolRepository">DAL repository to retrieve tool information.</param>
        /// <param name="profileRepository">DAL repository to retrieve profile information.</param>
        /// <param name="messageHistoryRepository">DAL repository to retrieve conversation history for previous completions.</param>
        /// <param name="indexMetaRepository">DAL repository to retrieve information about existing RAG tables.</param>
        public CompletionLogic(
            IAGIClientFactory agiClientFactory,
            IRagClientFactory ragClientFactory,
            IToolClient toolClient,
            IToolRepository toolRepository,
            IProfileRepository profileRepository,
            IMessageHistoryRepository messageHistoryRepository,
            IIndexMetaRepository indexMetaRepository)
        {
            _toolDb = toolRepository;
            _ToolClient = toolClient;
            _profileDb = profileRepository;
            _messageHistoryRepository = messageHistoryRepository;
            _ragMetaRepository = indexMetaRepository;
            _ragClientFactory = ragClientFactory;
            _agiClientFactory = agiClientFactory;
        }

        #region Streaming

        /// <summary>
        /// Streams completion updates to the requesting client.
        /// </summary>
        /// <param name="completionRequest">The body of the completion request.</param>
        /// <returns>An asynchronous enumerable that contains the completion response generated from an AGI client.</returns>
        public async IAsyncEnumerable<APIResponseWrapper<CompletionStreamChunk>> StreamCompletion(CompletionRequest completionRequest)
        {
            if (completionRequest.ProfileOptions == null || string.IsNullOrEmpty(completionRequest.ProfileOptions.Name))
            {
                yield return APIResponseWrapper<CompletionStreamChunk>.Failure("The required property 'Name' was not found in the ProfileOptions.", APIResponseStatusCodes.BadRequest);
                yield break;
            }

            var profile = await _profileDb.GetByNameAsync(completionRequest.ProfileOptions.Name);
            if (profile == null)
            {
                yield return APIResponseWrapper<CompletionStreamChunk>.Failure($"The profile '{completionRequest.ProfileOptions.Name}' was not found in the database.", APIResponseStatusCodes.NotFound);
                yield break;
            }

            var mappedProfile = DbMappingHandler.MapFromDbProfile(profile);
            completionRequest.ProfileOptions = await BuildCompletionOptions(mappedProfile, completionRequest.ProfileOptions);

            // Get and attach message history if a conversation id exists
            if (completionRequest.ConversationId is Guid conversationId)
            {
                completionRequest.Messages = await BuildMessageHistory(
                    conversationId,
                    completionRequest.Messages,
                    completionRequest.ProfileOptions.MaxMessageHistory);
            }

            // construct AGI Client based on the required host
            if (completionRequest.ProfileOptions?.Host == null)
            {
                yield return APIResponseWrapper<CompletionStreamChunk>.Failure("The required property 'Host' was not found in the Profile or ProfileOptions.", APIResponseStatusCodes.BadRequest);
                yield break;
            }
            var agiClient = _agiClientFactory.GetClient(completionRequest.ProfileOptions.Host);

            // Add data retrieved from RAG indexing
            if (!string.IsNullOrEmpty(completionRequest.ProfileOptions.RagDatabase))
            {
                var completionMessage = completionRequest.Messages.LastOrDefault();
                if (completionMessage == null)
                {
                    yield return APIResponseWrapper<CompletionStreamChunk>.Failure("The completion request must contain a message, but none were found.", APIResponseStatusCodes.BadRequest);
                    yield break;
                }

                var completionMessageWithRagData = await RetrieveRagData(completionRequest.ProfileOptions.RagDatabase, completionRequest, agiClient);
                if (completionMessageWithRagData != null)
                {
                    completionRequest.Messages.Remove(completionMessage);
                    completionRequest.Messages.Add(completionMessageWithRagData);
                }
            }

            var completionCollection = agiClient.StreamCompletion(completionRequest);
            if (completionCollection == null)
            {
                yield return APIResponseWrapper<CompletionStreamChunk>.Failure("An unknown error was encountered when trying to generate a completion. Please validate your API credentials, and rate limits.", APIResponseStatusCodes.InternalError);
                yield break;
            }

            Role? dbMessageRole;
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
                    dbMessageRole = roleString.ConvertStringToRole();
                    update.Role = dbMessageRole;
                }
                yield return APIResponseWrapper<CompletionStreamChunk>.Success(update);
            }

            if (toolCallDictionary.Any())
            {
                if (!string.IsNullOrEmpty(allCompletionChunks)) completionRequest.Messages.Add(new Message() { Content = allCompletionChunks, Role = Role.Assistant, User = _defaultUser, TimeStamp = DateTime.UtcNow });
                var wrappedResponse = await ExecuteTools(toolCallDictionary, completionRequest.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId);
                var (toolExecutionResponses, recursiveMessages) = wrappedResponse.Data;

                var completionString = string.Empty;
                if (recursiveMessages.Any()) foreach (var message in recursiveMessages) completionString += $"{message.User}: {message.Content}\n;";
                if (toolExecutionResponses.Any())
                {
                    var responseChunk = new CompletionStreamChunk()
                    {
                        CompletionUpdate = completionString,
                        ToolCalls = toolCallDictionary,
                        ToolExecutionResponses = toolExecutionResponses,
                        FinishReason = FinishReasons.ToolCalls,
                    };
                    yield return APIResponseWrapper<CompletionStreamChunk>.Success(responseChunk);
                }
            }

            // save new messages to database
            if (completionRequest.ConversationId is Guid id)
            {
                var lastUserMessage = completionRequest.Messages.Last(m => m.Role == Role.User);
                var dbUserMessage = DbMappingHandler.MapToDbMessage(new Message() { Role = Role.User, Content = lastUserMessage.Content, TimeStamp = lastUserMessage.TimeStamp, User = _defaultUser }, id);
                await _messageHistoryRepository.AddAsync(dbUserMessage);

                var dbMessage = DbMappingHandler.MapToDbMessage(new Message() { Role = Role.Assistant, Content = allCompletionChunks, TimeStamp = DateTime.UtcNow, User = completionRequest.ProfileOptions.Name ?? string.Empty }, id);
                await _messageHistoryRepository.AddAsync(dbMessage);
            }
        }

        #endregion

        #region Controller

        /// <summary>
        /// Processes a completion request and returns the completion response.
        /// </summary>
        /// <param name="completionRequest">The body of the completion request.</param>
        /// <returns>An <see cref="APIResponseWrapper{CompletionResponse}"/> containing a completion generated by an AGI client.</returns>
        public async Task<APIResponseWrapper<CompletionResponse>> ProcessCompletion(CompletionRequest completionRequest)
        {
            if (string.IsNullOrEmpty(completionRequest.ProfileOptions.Name)) return APIResponseWrapper<CompletionResponse>.Failure($"The required property ProfileOptions.Name is required.", APIResponseStatusCodes.BadRequest);
            if (!completionRequest.Messages.Any() && completionRequest.Messages.Exists(x => x.Role == Role.User)) return APIResponseWrapper<CompletionResponse>.Failure($"The messages array contain at least one User message.", APIResponseStatusCodes.BadRequest);

            var profile = await _profileDb.GetByNameAsync(completionRequest.ProfileOptions.Name);
            if (profile == null) return APIResponseWrapper<CompletionResponse>.Failure($"The profile '{completionRequest.ProfileOptions.Name}' was not found in the database.", APIResponseStatusCodes.NotFound);

            var mappedProfile = DbMappingHandler.MapFromDbProfile(profile);
            if (string.IsNullOrEmpty(mappedProfile.Model)) return APIResponseWrapper<CompletionResponse>.Failure($"The profile '{completionRequest.ProfileOptions.Model}' does not have a model associated with it.", APIResponseStatusCodes.BadRequest);
            if (string.IsNullOrEmpty(mappedProfile.Host.ToString())) return APIResponseWrapper<CompletionResponse>.Failure($"The profile '{completionRequest.ProfileOptions.Name}' does not have a host associated with it.", APIResponseStatusCodes.BadRequest);
            completionRequest.ProfileOptions = await BuildCompletionOptions(mappedProfile, completionRequest.ProfileOptions);

            if (completionRequest.ConversationId is Guid conversationId)
            {
                completionRequest.Messages = await BuildMessageHistory(
                    conversationId,
                    completionRequest.Messages,
                    completionRequest.ProfileOptions.MaxMessageHistory);
            }

            // construct AGI Client based on the required host
            if (completionRequest.ProfileOptions?.Host == null) return APIResponseWrapper<CompletionResponse>.Failure($"The required property 'Host' was not found in the Profile or ProfileOptions.", APIResponseStatusCodes.BadRequest);
            var agiClient = _agiClientFactory.GetClient(completionRequest.ProfileOptions.Host);

            if (!string.IsNullOrEmpty(completionRequest.ProfileOptions.RagDatabase))
            {
                var completionMessageWithRagData = await RetrieveRagData(completionRequest.ProfileOptions.RagDatabase, completionRequest, agiClient);
                if (completionMessageWithRagData != null)
                {
                    var completionMessage = completionRequest.Messages.LastOrDefault();
                    completionRequest.Messages.Remove(completionMessage);
                    completionRequest.Messages.Add(completionMessageWithRagData);
                }
            }

            var completion = await agiClient.PostCompletion(completionRequest);
            if (completion.FinishReason == FinishReasons.Error) return APIResponseWrapper<CompletionResponse>.Failure(completion, "An error was encountered when trying to generate a completion.", APIResponseStatusCodes.InternalError);
            if (completion.FinishReason == FinishReasons.TooManyRequests) return APIResponseWrapper<CompletionResponse>.Failure(completion, $"The Host service quota has been exceeded. Please check your API quota details for '{completionRequest.ProfileOptions.Host}'.", APIResponseStatusCodes.TooManyRequests);

            if (completionRequest.ConversationId is Guid id)
            {
                var compUserMessage = completion.Messages.Last(m => m.Role == Role.User);
                compUserMessage.User = _defaultUser;
                var dbUserMessage = DbMappingHandler.MapToDbMessage(compUserMessage, id);
                await _messageHistoryRepository.AddAsync(dbUserMessage);

                var compMessage = completion.Messages.Last(m => m.Role == Role.Assistant || m.Role == Role.Tool);
                compMessage.User = completionRequest.ProfileOptions.Name;
                var dbMessage = DbMappingHandler.MapToDbMessage(compMessage, id);
                await _messageHistoryRepository.AddAsync(dbMessage);
            }

            if (completion.FinishReason == FinishReasons.ToolCalls)
            {
                var wrappedResponse = await ExecuteTools(completion.ToolCalls, completion.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId);
                var (toolExecutionResponses, recursiveMessages) = wrappedResponse.Data;
                if (toolExecutionResponses.Any()) completion.ToolExecutionResponses.AddRange(toolExecutionResponses);
                if (recursiveMessages.Any()) completion.Messages = recursiveMessages;
            }
            return APIResponseWrapper<CompletionResponse>.Success(completion);
        }

        /// <summary>
        /// Retrieves and builds the message history for a conversation.
        /// </summary>
        /// <param name="conversationId">The Id of the conversation being retrieved from the database.</param>
        /// <param name="requestMessages">The list of messages associated with the current completion request.</param>
        /// <param name="maxMessageHistory">The maximum amount of messages to include in the list of messages.</param>
        /// <returns>A list of messages associated with the conversationId.</returns>
        private async Task<List<Message>> BuildMessageHistory(Guid conversationId, List<Message> requestMessages, int? maxMessageHistory = null)
        {
            var allMessages = new List<Message>();
            var messageHistory = await _messageHistoryRepository.GetConversationAsync(conversationId, maxMessageHistory ?? _defaultMessageHistory, 1);
            if (messageHistory == null || messageHistory.Count < 1) return requestMessages; // no conversation found, return original data and create conversation entry later

            var mappedMessageHistory = new List<Message>();
            foreach (var message in messageHistory) mappedMessageHistory.Add(DbMappingHandler.MapFromDbMessage(message));

            // ensure the messages are properly arranged, with the user completion appearing very last
            allMessages.AddRange(mappedMessageHistory);
            allMessages.AddRange(requestMessages);

            return allMessages;
        }

        /// <summary>
        /// Builds the completion options utilizing an existing profile, and overriding these options
        /// in the request body.
        /// </summary>
        /// <param name="profile">The profile as stored in the database.</param>
        /// <param name="profileOptions">The profile options in the request body that can be used to override existing settings.</param>
        /// <returns>An updated <see cref="Profile"/> with any existing, or overridden values.</returns>
        public async Task<Profile> BuildCompletionOptions(Profile profile, Profile? profileOptions = null)
        {
            // adds the profile references to the tools
            var profileReferences = profileOptions?.ReferenceProfiles ?? profile.ReferenceProfiles ?? Array.Empty<string>();
            if (profile.Tools == null) profile.Tools = new List<Tool>();

            // add image gen system tool if appropriate - Anthropic does not support image gen currently
            var host = string.IsNullOrEmpty(profileOptions?.ImageHost.ToString()) ? profile.ImageHost : profileOptions?.ImageHost ?? profileOptions?.Host ?? profile.Host;
            if (host != AGIServiceHost.Anthropic && host != AGIServiceHost.None) profile.Tools.Add(new ImageGenSystemTool());

            // add recursive chat system tool if appropriate
            if (profileReferences != null && profileReferences.Any())
            {
                // add to both tool sets to ensure no overwrites occur
                var recursionTool = await BuildProfileReferenceTool(profileReferences);
                profileOptions?.Tools?.Add(recursionTool);
                profile.Tools.Add(recursionTool);
            }

            // overwrite defaults if ProfileOptions contains values
            return new Profile()
            {
                Id = profile.Id,
                Name = string.IsNullOrEmpty(profileOptions?.Name) ? profile.Name : profileOptions?.Name,
                Model = string.IsNullOrEmpty(profileOptions?.Model) ? profile.Model : profileOptions?.Model,
                RagDatabase = string.IsNullOrEmpty(profileOptions?.RagDatabase) ? profile.RagDatabase : profileOptions?.RagDatabase,
                MaxMessageHistory = profileOptions?.MaxMessageHistory ?? profile.MaxMessageHistory,
                MaxTokens = profileOptions?.MaxTokens ?? profile.MaxTokens,
                Temperature = profileOptions?.Temperature ?? profile.Temperature,
                TopP = profileOptions?.TopP ?? profile.TopP,
                FrequencyPenalty = profileOptions?.FrequencyPenalty ?? profile.FrequencyPenalty,
                PresencePenalty = profileOptions?.PresencePenalty ?? profile.PresencePenalty,
                Stop = string.IsNullOrEmpty(profileOptions?.Stop?.ToCommaSeparatedString()) ? profile.Stop : profileOptions?.Stop,
                Logprobs = profileOptions?.Logprobs ?? profile.Logprobs,
                TopLogprobs = profileOptions?.TopLogprobs ?? profile.TopLogprobs,
                ResponseFormat = string.IsNullOrEmpty(profileOptions?.ResponseFormat) ? profile.ResponseFormat : profileOptions?.ResponseFormat,
                User = string.IsNullOrEmpty(profileOptions?.User) ? profile.User : profileOptions?.User,
                Tools = profileOptions?.Tools ?? profile.Tools,
                SystemMessage = string.IsNullOrEmpty(profileOptions?.SystemMessage) ? profile.SystemMessage : profileOptions?.SystemMessage,
                ReferenceProfiles = profileReferences,
                Host = profileOptions?.Host ?? profile.Host,
                ToolChoice = string.IsNullOrEmpty(profileOptions?.ToolChoice) ? profile.ToolChoice : profileOptions?.ToolChoice
            };
        }
        #endregion

        #region Shared

        /// <summary>
        /// Builds a recursive chat system tool based on the profile references parameter.
        /// </summary>
        /// <param name="profileNames">The profiles used to create a recursive chat tool.</param>
        /// <returns>A <see cref="RecursiveChatSystemTool"/> that can be called by AGI client models.</returns>
        private async Task<RecursiveChatSystemTool> BuildProfileReferenceTool(string[] profileNames)
        {
            var referenceProfiles = new List<Profile>();
            foreach (var name in profileNames)
            {
                var profile = await _profileDb.GetByNameAsync(name);
                if (profile == null) continue;
                referenceProfiles.Add(DbMappingHandler.MapFromDbProfile(profile));
            }
            return new RecursiveChatSystemTool(referenceProfiles);
        }

        /// <summary>
        /// Processes and executes any tool calls that are present in a completion response.
        /// </summary>
        /// <param name="toolCalls">A dictionary of function names, and their arguments.</param>
        /// <param name="messages">The conversation history used as context for the Chat Recursion system tool.</param>
        /// <param name="options">The AI client profile options associated with the request being processed.</param>
        /// <param name="conversationId">The Id of the conversation being processed.</param>
        /// <param name="currentRecursionDepth">The current depth of recursion used to prevent infinite looping
        /// resulting from the Chat Recursion system tool.</param>
        /// <returns>An <see cref="APIResponseWrapper{Tuple}"/> containing a tuple of the HTTP responses associated with tools executed by the tool client, and any 
        /// new messages generated from the Chat Recursion system tool.</returns>
        public async Task<APIResponseWrapper<(List<HttpResponseMessage>, List<Message>)>> ExecuteTools(Dictionary<string, string> toolCalls, List<Message> messages, Profile? options = null, Guid? conversationId = null, int currentRecursionDepth = 0)
        {
            var functionResults = new List<HttpResponseMessage>();
            var maxDepth = options?.MaxMessageHistory ?? _defaultMaxRecursionMessageHistory;
            foreach (var tool in toolCalls)
            {
                if (tool.Key.ToLower().Equals(SystemTools.Chat_Recursion.ToString().ToLower()) || currentRecursionDepth > maxDepth) messages = await HandleRecursiveChat(tool.Value, messages, options, conversationId, currentRecursionDepth + 1);
                else if (tool.Key.ToLower().Equals(SystemTools.Image_Gen.ToString().ToLower()) || currentRecursionDepth > maxDepth) messages = await GenerateImage(tool.Value, options?.ImageHost ?? options?.Host, messages);
                else
                {
                    var dbTool = await _toolDb.GetByNameAsync(tool.Key);
                    if (dbTool != null && !string.IsNullOrEmpty(dbTool.ExecutionUrl)) functionResults.Add(await _ToolClient.CallFunction(tool.Key, tool.Value, dbTool.ExecutionUrl, dbTool.ExecutionMethod, dbTool.ExecutionBase64Key));
                }
            }
            return APIResponseWrapper<(List<HttpResponseMessage>, List<Message>)>.Success((functionResults, messages));
        }

        /// <summary>
        /// Handles the recursive chat system tool by calling the appropriate AI model
        /// and managing completion context.
        /// </summary>
        /// <param name="toolCall">The arguments passed to the Recursive Chat tool.</param>
        /// <param name="messages">A list of messages representing the conversation context.</param>
        /// <param name="options">The options associated with the current profile to generate the next completion.</param>
        /// <param name="conversationId">The Id of the conversation if the conversation is being saved.</param>
        /// <param name="currentRecursionDepth">The current depth of recursion used to prevent infinite looping
        /// resulting from the Chat Recursion system tool.</param>
        /// <returns>An updated <see cref="List{Message}"/> representing the conversation context.</returns>
        private async Task<List<Message>> HandleRecursiveChat(string toolCall, List<Message> messages, Profile? options = null, Guid? conversationId = null, int currentRecursionDepth = 0)
        {
            var toolExecutionCall = JsonSerializer.Deserialize<RecursiveChatSystemToolExecutionCall>(toolCall);
            if (toolExecutionCall == null) return messages;

            var profileName = toolExecutionCall.responding_ai_model;
            if (string.IsNullOrEmpty(profileName)) return messages;

            var recursionProfile = await _profileDb.GetByNameAsync(profileName);
            if (recursionProfile == null) return messages;

            var completionRequest = new CompletionRequest()
            {
                ConversationId = conversationId,
                Messages = messages,
                ProfileOptions = DbMappingHandler.MapFromDbProfile(recursionProfile)
            };

            var maxMessageHistory = completionRequest.ProfileOptions.MaxMessageHistory ?? _defaultMaxRecursionMessageHistory;
            if (currentRecursionDepth < maxMessageHistory)
            {
                var recursionCompletion = await ProcessRecursiveCompletion(completionRequest, currentRecursionDepth);
                if (recursionCompletion != null) messages = recursionCompletion.Messages;
            }
            return messages;
        }

        /// <summary>
        /// Processes a recursive completion request and returns the completion response.
        /// </summary>
        /// <param name="completionRequest">The request body for the request.</param>
        /// <param name="currentRecursionDepth">The current depth of recursion used to prevent infinite loops.</param>
        /// <returns>A <see cref="CompletionResponse?"> containing the AGI client's response.</returns>
        private async Task<CompletionResponse?> ProcessRecursiveCompletion(CompletionRequest completionRequest, int currentRecursionDepth)
        {
            if (!completionRequest.Messages.Any() || string.IsNullOrEmpty(completionRequest.ProfileOptions.Name)) return null;

            // reorganize and combine messages of same role in order to reduce model performance deprecation
            var MessagesAfterRoleDistribution = new List<Message>();
            var messages = completionRequest.Messages;

            // copy the list, creating a seperate ref
            var originalMessageDistribution = new List<Message>(completionRequest.Messages.Select(m => new Message 
            {
                Role = m.Role,
                User = m.User,
                Content = m.Content,
                Base64Image = m.Base64Image,
                TimeStamp = m.TimeStamp
            }));
            
            for (var i = 0; i < messages.Count; i++)
            {
                // Determine the role of the current message
                if (i == messages.Count - 1) messages[i].Role = Role.User; // Set the last message as User
                else if (messages[i].User.ToLower() == completionRequest.ProfileOptions.Name.ToLower()) messages[i].Role = Role.Assistant; // Set as Assistant
                else messages[i].Role = Role.User; // Set as User

                // Combine consecutive messages with the same role or add the message to the list if the role is different
                if (MessagesAfterRoleDistribution.Count > 0 && MessagesAfterRoleDistribution[^1].Role == messages[i].Role) MessagesAfterRoleDistribution[^1].Content += "\n\n" + messages[i].Content;
                else MessagesAfterRoleDistribution.Add(messages[i]);
            }

            completionRequest.Messages = MessagesAfterRoleDistribution;

            // Build Options
            completionRequest.ProfileOptions = await BuildCompletionOptions(completionRequest.ProfileOptions);

            // construct AGI Client based on the required host
            var agiClient = _agiClientFactory.GetClient(completionRequest.ProfileOptions.Host);

            // Add data retrieved from RAG indexing
            if (!string.IsNullOrEmpty(completionRequest.ProfileOptions.RagDatabase) && completionRequest.Messages.Any())
            {
                var completionMessage = completionRequest.Messages.LastOrDefault() ?? new Message() { Role = Role.User, User = _defaultUser, Content = string.Empty, TimeStamp = DateTime.UtcNow };
                var completionMessageWithRagData = await RetrieveRagData(completionRequest.ProfileOptions.RagDatabase, completionRequest, agiClient);
                if (completionMessageWithRagData != null)
                {
                    completionRequest.Messages.Remove(completionMessage);
                    completionRequest.Messages.Add(completionMessageWithRagData);
                }
            }

            var completion = await agiClient.PostCompletion(completionRequest);
            if (completion.FinishReason == FinishReasons.Error) return completion;

            if (completionRequest.ConversationId is Guid id)
            {
                var dbMessage = DbMappingHandler.MapToDbMessage(completion.Messages.Last(m => m.Role == Role.Assistant || m.Role == Role.Tool), id);
                dbMessage.User = completionRequest.ProfileOptions.Name ?? string.Empty;
                await _messageHistoryRepository.AddAsync(dbMessage);
            }

            // restore original message distribution with the latest data appended
            originalMessageDistribution.Add(completion.Messages.Last(m => m.Role == Role.Assistant || m.Role == Role.Tool));
            completion.Messages = originalMessageDistribution;
            completion.Messages.Last().User = completionRequest.ProfileOptions.Name;
            
            if (completion.FinishReason == FinishReasons.ToolCalls)
            {
                var wrappedResponse = await ExecuteTools(completion.ToolCalls, completion.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId, currentRecursionDepth: currentRecursionDepth);
                var (toolExecutionResponses, recursiveMessages) = wrappedResponse.Data;
                if (toolExecutionResponses.Any()) completion.ToolExecutionResponses.AddRange(toolExecutionResponses);
                if (recursiveMessages.Any()) completion.Messages = recursiveMessages;
            }
            return completion;
        }

        /// <summary>
        /// Retrieves RAG data from the specified index and appends it to the last message in the completion request.
        /// </summary>
        /// <param name="indexName">The name of the RAG index to search.</param>
        /// <param name="originalRequest">The original completion request used to generate a search intent.</param>
        /// <param name="agiClient">The AGI client that will be used to generate a search intent.</param>
        /// <returns>The original <see cref="Message?"> with any relevant RAG documents attached in the content.</returns>
        private async Task<Message?> RetrieveRagData(string indexName, CompletionRequest originalRequest, IAGIClient agiClient)
        {
            var dbIndex = await _ragMetaRepository.GetByNameAsync(indexName);
            if (!originalRequest.Messages.Any() || dbIndex == null) return null;


            var intentfulQuery = await GenerateIntentfulQuery(agiClient, originalRequest);
            if (string.IsNullOrWhiteSpace(intentfulQuery)) return null;

            var indexData = DbMappingHandler.MapFromDbIndexMetadata(dbIndex);
            var ragClient = _ragClientFactory.GetClient(indexData.RagHost);
            var ragData = await ragClient.SearchIndex(indexData, intentfulQuery);

            var sb = new StringBuilder();
            await foreach (var item in ragData.GetResultsAsync())
            {
                var doc = item.Document;
                sb.AppendLine("```");
                sb.AppendLine($"Title: {doc.title}");
                sb.AppendLine($"Source: {doc.source}");
                sb.AppendLine($"Creation Date: {doc.created:yyyy-MM-ddTHH:mm:ss}");
                sb.AppendLine($"Last Updated Date: {doc.modified:yyyy-MM-ddTHH:mm:ss}");
                sb.AppendLine("Content:");
                if (indexData.QueryType == QueryType.Semantic) foreach (var cap in item.SemanticSearch.Captions) sb.AppendLine($"Excerpt: {cap.Text}\n");
                else sb.AppendLine(doc.chunk);
                sb.AppendLine("```");
            }

            var ragDataString = sb.ToString();
            if (string.IsNullOrEmpty(ragDataString)) return null;

            return new Message
            {
                Role = originalRequest.Messages.Last().Role,
                User = originalRequest.Messages.Last().User,
                Base64Image = originalRequest.Messages.Last().Base64Image,
                TimeStamp = originalRequest.Messages.Last().TimeStamp,
                Content = RagRequestPrependedInstructions
                            + "\n\n"
                            + originalRequest.Messages.Last().Content
                            + "\n\n"
                            + ragDataString
            };
        }

        /// <summary>
        /// Generates an intentful query based on the original request's messages and other context.
        /// </summary>
        /// <param name="originalRequest">The original Completion used to start this request.</param>
        /// <param name="agiClient">The AGI client that will be used to generate a search intent.</param>
        /// <returns>Returns an intentful query as a string that can be used to search a RAG database.</returns>
        private async Task<string> GenerateIntentfulQuery(IAGIClient agiClient, CompletionRequest originalRequest)
        {
            var additionalContext = $"Provided below is a system message that the final LLM in this AI orchestration pipeline uses to answer the user's question, " +
                $"after your intent is used to retrieve relevant documents. Please use this context, and any other existing message history in order to construct " +
                $"a short search intent string similar to an internet search in order to retrieve these documents, providing keywords as the primary input " +
                $"whenever possible." +
                $"\n\n" +
                $"Final LLM Name: {originalRequest.ProfileOptions.Name}" +
                $"Final LLM System Message: {originalRequest.ProfileOptions.SystemMessage}";

            var modifiedRequest = new CompletionRequest
            {
                ProfileOptions = new Profile { Model = originalRequest.ProfileOptions.Model, SystemMessage = RagIntentGenSystemMessage },
                Messages = ShallowCloneAndModifyLast(originalRequest.Messages, additionalContext)
            };

            var completionResponse = await agiClient.PostCompletion(modifiedRequest);
            var intentfulQuery = completionResponse.Messages.LastOrDefault()?.Content ?? string.Empty;
            return intentfulQuery;
        }

        /// <summary>
        /// Shallow-clones the given list of messages, but replaces the last message
        /// with a new Message whose Content is prefix + original.Content.
        /// </summary>
        private List<Message> ShallowCloneAndModifyLast(IList<Message> originalMessages, string prefix)
        {
            var cloned = new List<Message>(originalMessages.Count);
            if (originalMessages == null || originalMessages.Count == 0)
                return cloned;

            // Copy everything except the last message
            for (int i = 0; i < originalMessages.Count - 1; i++)
            {
                var m = originalMessages[i];
                cloned.Add(new Message
                {
                    Role = m.Role,
                    Content = m.Content,
                    TimeStamp = m.TimeStamp,
                    User = m.User,
                    Base64Image = m.Base64Image
                });
            }

            // Create a brand-new last message with modified content
            var last = originalMessages[^1];
            cloned.Add(new Message
            {
                Role = last.Role,
                Content = prefix + last.Content,
                TimeStamp = last.TimeStamp,
                User = last.User,
                Base64Image = last.Base64Image
            });

            return cloned;
        }


        /// <summary>
        /// Generates an image based on the prompt provided in the completion request.
        /// </summary>
        /// <param name="imageGenArgs">The arguments associated with an image generation tool call.</param>
        /// <param name="host">The name of the AGI client that will be used to generate the image.</param>
        /// <param name="messages">A list of messages representing the current conversation context.</param>
        /// <returns>A <see cref="List{Message}"> representing the current conversation context with the generated 
        /// image attached.</returns>
        private async Task<List<Message>> GenerateImage(string imageGenArgs, AGIServiceHost? host, List<Message> messages)
        {
            // anthropic does not support image gen currently
            if (host == AGIServiceHost.Anthropic) return messages;

            var imageGenArgsJson = JsonSerializer.Deserialize<Dictionary<string, string>>(imageGenArgs);
            if (imageGenArgsJson == null || !imageGenArgsJson.ContainsKey("prompt")) return messages;

            var prompt = imageGenArgsJson["prompt"];

            var client = _agiClientFactory.GetClient(host);
            var base64Image = await client.GenerateImage(prompt);

            // Append the generated image to the last message
            var lastMessage = messages.LastOrDefault();
            if (lastMessage != null && !string.IsNullOrEmpty(base64Image)) lastMessage.Base64Image = base64Image ?? string.Empty;
            else if (!string.IsNullOrEmpty(base64Image)) messages.Add(new Message() { Content = string.Empty, Role = Role.Assistant, TimeStamp = DateTime.UtcNow, Base64Image = base64Image ?? string.Empty });
            return messages;
        }
        #endregion
    }
}
