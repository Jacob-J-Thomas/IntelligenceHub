using Azure.AI.OpenAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenAI.Assistants;
using OpenAI.Chat;
using IntelligenceHub.API.DTOs.ClientDTOs.AICompletionDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs.Response;
using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs;
using IntelligenceHub.API.MigratedDTOs;
using IntelligenceHub.Common;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http.Headers;
using System.Text;
using static IntelligenceHub.Common.GlobalVariables;

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

        public async Task<ChatCompletion?> PostCompletion(CompletionRequest completionRequest)
        {
            var options = BuildCompletionOptions(completionRequest);
            var messages = BuildCompletionMessages(completionRequest);
            var chatClient = _azureOpenAIClient.GetChatClient(completionRequest.ProfileOptions.Model);
            return await chatClient.CompleteChatAsync(messages, options);
        }

        public AsyncCollectionResult<StreamingChatCompletionUpdate> StreamCompletion(CompletionRequest completionRequest)
        {
            var options = BuildCompletionOptions(completionRequest);
            var messages = BuildCompletionMessages(completionRequest);
            var chatClient = _azureOpenAIClient.GetChatClient(completionRequest.ProfileOptions.Model);
            return chatClient.CompleteChatStreamingAsync(messages, options);
        }

        private List<ChatMessage> BuildCompletionMessages(CompletionRequest completionRequest)
        {
            var systemMessage = completionRequest.ProfileOptions.System_Message;
            var completionMessages = new List<ChatMessage>();
            if (!string.IsNullOrWhiteSpace(systemMessage)) completionMessages.Add(new SystemChatMessage(systemMessage));
            foreach (var message in completionRequest.Messages)
            {
                if (message.Role == MessageRole.User.ToString()) completionMessages.Add(new UserChatMessage(message.Content));
                else if (message.Role == MessageRole.Assistant.ToString()) completionMessages.Add(new AssistantChatMessage(message.Content));
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

            return options;
        }
    }
}
