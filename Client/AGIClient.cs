using Azure.AI.OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;
using IntelligenceHub.API.MigratedDTOs;
using System.ClientModel;
using static IntelligenceHub.Common.GlobalVariables;
using OpenAICustomFunctionCallingAPI.API.MigratedDTOs;
using IntelligenceHub.Common.Exceptions;
using IntelligenceHub.Common;
using OpenAI.Embeddings;
using IntelligenceHub.API.DTOs.ClientDTOs.EmbeddingDTOs;
using Azure.AI.OpenAI.Chat;

namespace IntelligenceHub.Client
{
    public class AGIClient
    {
        private AzureOpenAIClient _azureOpenAIClient;

        public AGIClient(string apiEndpoint, string apiKey) 
        {
            var endpointWithRouting = apiEndpoint + "chat/completions";
            var resourceUri = new Uri(endpointWithRouting);
            var credential = new ApiKeyCredential(apiKey);
            _azureOpenAIClient = new AzureOpenAIClient(resourceUri, credential);
        }

        public async Task<CompletionResponse?> PostCompletion(CompletionRequest completionRequest)
        {
            var options = BuildCompletionOptions(completionRequest);
            var messages = BuildCompletionMessages(completionRequest);
            var chatClient = _azureOpenAIClient.GetChatClient(completionRequest.ProfileOptions.Model);
            var completionResult = await chatClient.CompleteChatAsync(messages, options);

            var toolCalls = new Dictionary<string, string>();
            foreach (var tool in completionResult.Value.ToolCalls) toolCalls.Add(tool.FunctionName, tool.FunctionArguments);

            // build the response object
            var responseMessage = new Message()
            {
                Content = completionResult.Value.Content.ToString() ?? string.Empty,
                Role = GlobalVariables.ConvertStringToRole(completionResult.Value.Role.ToString())
            };

            var response = new CompletionResponse()
            {
                FinishReason = ConvertStringToFinishReason(completionResult.Value.FinishReason.ToString()),
                Messages = completionRequest.Messages,
                ToolCalls = toolCalls
            };
            response.Messages.Add(responseMessage);
            return response;
        }

        public async IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest)
        {
            var options = BuildCompletionOptions(completionRequest);
            var messages = BuildCompletionMessages(completionRequest);
            var chatClient = _azureOpenAIClient.GetChatClient(completionRequest.ProfileOptions.Model);
            var resultCollection = chatClient.CompleteChatStreamingAsync(messages, options);

            var chunkId = 0;
            var role = string.Empty;
            var finishReason = string.Empty;
            var currentTool = string.Empty;
            var currentToolArgs = string.Empty;
            var toolCalls = new Dictionary<string, string>();
            await foreach (var result in resultCollection)
            {
                role = result.Role.ToString() ?? role;
                finishReason = result.FinishReason.ToString() ?? finishReason;
                var content = result.ContentUpdate.ToString() ?? string.Empty;

                // handle tool conversion - move to seperate method
                foreach (var update in result.ToolCallUpdates)
                {
                    // capture current values
                    if (string.IsNullOrEmpty(currentTool)) currentTool = update.FunctionName;
                    if (currentTool == update.FunctionName) currentToolArgs += update.FunctionArgumentsUpdate;
                    else
                    {
                        currentTool = update.FunctionName;
                        currentToolArgs = update.FunctionArgumentsUpdate;
                    }

                    if (toolCalls.ContainsKey(currentTool)) toolCalls[currentTool] = currentToolArgs;
                    else toolCalls.Add(currentTool, currentToolArgs);
                }

                yield return new CompletionStreamChunk()
                {
                    Id = chunkId++,
                    Role = GlobalVariables.ConvertStringToRole(role),
                    CompletionUpdate = content,
                    FinishReason = ConvertStringToFinishReason(finishReason),
                    ToolCalls = toolCalls
                };
            }
        }

        public async Task<float[]?> GetEmbeddings(EmbeddingRequestBase completion, string embeddingModel = "text-embedding-3-small")
        {
            var embeddingClient = _azureOpenAIClient.GetEmbeddingClient(embeddingModel);
            var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(completion.Input);
            return embeddingResponse.Value.Vector.ToArray();
        }

        private List<ChatMessage> BuildCompletionMessages(CompletionRequest completionRequest)
        {
            var systemMessage = completionRequest.ProfileOptions.System_Message;
            var completionMessages = new List<ChatMessage>();
            if (!string.IsNullOrWhiteSpace(systemMessage)) completionMessages.Add(new SystemChatMessage(systemMessage));
            foreach (var message in completionRequest.Messages)
            {
                if (message.Role.ToString() == MessageRole.User.ToString()) completionMessages.Add(new UserChatMessage(message.Content));
                else if (message.Role.ToString() == MessageRole.Assistant.ToString()) completionMessages.Add(new AssistantChatMessage(message.Content));
            }
            return completionMessages;
        }

        private ChatCompletionOptions BuildCompletionOptions(CompletionRequest completion)
        {
            var options = new ChatCompletionOptions()
            {
                MaxTokens = completion.ProfileOptions.Max_Tokens,
                Temperature = completion.ProfileOptions.Temperature,
                TopP = completion.ProfileOptions.Top_P,
                FrequencyPenalty = completion.ProfileOptions.Frequency_Penalty,
                PresencePenalty = completion.ProfileOptions.Presence_Penalty,
                IncludeLogProbabilities = completion.ProfileOptions.Logprobs,

                // test if below works
                //ParallelToolCallsEnabled = true,
                
                Seed = completion.ProfileOptions.Seed,
                EndUserId = completion.ProfileOptions.User
            };

            // Potentially useful later for testing, validation, and fine tuning. Maps token probabilities
            //options.LogitBiases

            // set response format
            if (completion.ProfileOptions.Response_Format == ResponseFormat.Json.ToString()) options.ResponseFormat = ChatResponseFormat.JsonObject;
            else if (completion.ProfileOptions.Response_Format == ResponseFormat.Text.ToString()) options.ResponseFormat = ChatResponseFormat.Text;

            // set log probability
            if (options.IncludeLogProbabilities == true) options.TopLogProbabilityCount = completion.ProfileOptions.Top_Logprobs;

            // set stop messages
            if (completion.ProfileOptions.Stop != null && completion.ProfileOptions.Stop.Length > 0)
            {
                foreach (var message in completion.ProfileOptions.Stop) options.StopSequences.Add(message);
            }

            // set tools
            if (completion.ProfileOptions.Tools != null) foreach (var tool in completion.ProfileOptions.Tools) 
                {
                    options.Tools.Add(ChatTool.CreateFunctionTool(
                        tool.Function.Name, 
                        tool.Function.Description, 
                        new BinaryData(tool.Function.Parameters)));
                };

            // Set tool choice
            if (completion.ProfileOptions.Tool_Choice == null || completion.ProfileOptions.Tool_Choice == ToolExecutionRequirement.None.ToString()) options.ToolChoice = ChatToolChoice.None;
            else if (completion.ProfileOptions.Tool_Choice == ToolExecutionRequirement.Auto.ToString()) options.ToolChoice = ChatToolChoice.Auto;
            else if (completion.ProfileOptions.Tool_Choice == ToolExecutionRequirement.Required.ToString()) options.ToolChoice = ChatToolChoice.Required;

#pragma warning disable AOAI001
            if (!string.IsNullOrEmpty(completion.ProfileOptions.RagDatabase)) options = AttachDatabaseOptions(completion.ProfileOptions.RagDatabase, options);
            return options;
        }

        private ChatCompletionOptions AttachDatabaseOptions(string indexName, ChatCompletionOptions options)
        {
            var fieldMappings = new DataSourceFieldMappings();

            // configure below dynamically based off of RAG database definition
            fieldMappings.VectorFieldNames.Add("contentVector");
            fieldMappings.VectorFieldNames.Add("titleVector");

            // get below values from database
            options.AddDataSource(new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(_aiSearchServiceUrl), // retrieve from RagDB
                Authentication = DataSourceAuthentication.FromApiKey(_aiSearchServiceKey), // retrieve from RagDB
                IndexName = indexName, // create an Options property for API requests to hold this and below values
                InScope = false, // add to DatabaseOptions
                SemanticConfiguration = "semantic", // add to DatabaseOptions ?? defaultValue
                QueryType = "vector", // add to DatabaseOptions ?? defaultValue
                VectorizationSource = DataSourceVectorizer.FromDeploymentName("text-embedding-ada-002"), // add string to dbOptions
                FieldMappings = fieldMappings,

                // This should be set to the system message currently as the system message doesn't seem to work with RAG
                RoleInformation = "You are an office assistant who works for Convergint, and helps fellow colleagues by providing information about Convergint and its processes and resources, assist with brainstorming, troubleshooting, drafting templates, and more. The sources you are provided come from knowledge base articles from Convergint's helpdesk, sharepoint document stores, and user directory information. You should only reference these documents when they relate to the users question, otherwise simply use your own internal knowledge that you were trained on to complete their request.",

                // Add these
                TopNDocuments = 5, // get from databaseOptions ?? defaultValue
                OutputContextFlags = DataSourceOutputContextFlags.Citations | // probably just hard code value as this?
                    DataSourceOutputContextFlags.Intent |
                    DataSourceOutputContextFlags.AllRetrievedDocuments,

                Strictness = 4, // get from databaseOptions ?? null
                
                // not sure if we want to set this or not
                MaxSearchQueries = 5,


                // probably don't use below for now
                //AllowPartialResult = false,
                //Filter = // seems very useful
            });
        }
    }
}
