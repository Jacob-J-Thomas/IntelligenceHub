using Azure.AI.OpenAI;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common.Extensions;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Images;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Client.Implementations
{
    /// <summary>
    /// A client for interacting with Anthropic models deployed through Azure AI Foundry.
    /// </summary>
    public class AnthropicAIClient : IAGIClient
    {
        private AzureOpenAIClient _azureOpenAIClient;

        public AnthropicAIClient(IOptionsMonitor<AGIClientSettings> settings, IHttpClientFactory policyFactory)
        {
            var policyClient = policyFactory.CreateClient(ClientPolicies.AnthropicAIClientPolicy.ToString());

            var service = settings.CurrentValue.AnthropicServices.Find(service => service.Endpoint == policyClient.BaseAddress?.ToString())
                ?? throw new InvalidOperationException("service key failed to be retrieved when attempting to generate a completion.");

            var credential = new ApiKeyCredential(service.Key);
            var options = new AzureOpenAIClientOptions()
            {
                Transport = new HttpClientPipelineTransport(policyClient)
            };
            _azureOpenAIClient = new AzureOpenAIClient(policyClient.BaseAddress, credential, options);
        }

        public Task<string?> GenerateImage(string prompt)
        {
            // Anthropic models deployed via Foundry do not currently support image generation
            return Task.FromResult<string?>(null);
        }

        public async Task<CompletionResponse> PostCompletion(CompletionRequest completionRequest)
        {
            try
            {
                var options = BuildCompletionOptions(completionRequest);
                var messages = BuildCompletionMessages(completionRequest);
                var chatClient = _azureOpenAIClient.GetChatClient(completionRequest.ProfileOptions.Model);

                var completionResult = await chatClient.CompleteChatAsync(messages, options);

                var toolCalls = new Dictionary<string, string>();
                foreach (var tool in completionResult.Value.ToolCalls)
                {
                    if (tool.FunctionName.ToLower() != SystemTools.Chat_Recursion.ToString().ToLower()) toolCalls.Add(tool.FunctionName, tool.FunctionArguments.ToString());
                    else toolCalls.Add(SystemTools.Chat_Recursion.ToString().ToLower(), string.Empty);
                }

                var contentString = GetMessageContent(completionResult.Value.Content.FirstOrDefault()?.Text, toolCalls);

                var responseMessage = new Message()
                {
                    Content = contentString,
                    Role = completionResult.Value.Role.ToString().ConvertStringToRole() ?? Role.Assistant,
                    User = completionRequest.ProfileOptions.User ?? string.Empty,
                    TimeStamp = DateTime.UtcNow
                };

                foreach (var content in completionResult.Value.Content)
                {
                    if (responseMessage.Base64Image == null && content.Kind == ChatMessageContentPartKind.Image) responseMessage.Base64Image = Convert.ToBase64String(content.ImageBytes);
                    else if (string.IsNullOrEmpty(responseMessage.Content) && content.Kind == ChatMessageContentPartKind.Text) responseMessage.Content = content.Text;
                }

                var response = new CompletionResponse()
                {
                    FinishReason = completionResult.Value.FinishReason.ToString().ConvertStringToFinishReason(),
                    Messages = completionRequest.Messages,
                    ToolCalls = toolCalls
                };
                response.Messages.Add(responseMessage);
                return response ?? new CompletionResponse() { FinishReason = FinishReasons.Error };
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return new CompletionResponse() { FinishReason = FinishReasons.TooManyRequests };
            }
        }

        public async IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest)
        {
            var options = BuildCompletionOptions(completionRequest);
            var messages = BuildCompletionMessages(completionRequest);
            var chatClient = _azureOpenAIClient.GetChatClient(completionRequest.ProfileOptions.Model);
            var resultCollection = chatClient.CompleteChatStreamingAsync(messages, options);

            var chunkId = 0;
            string role = null;
            string finishReason = null;
            var currentTool = string.Empty;
            var currentToolArgs = string.Empty;
            var toolCalls = new Dictionary<string, string>();
            await foreach (var result in resultCollection)
            {
                if (!string.IsNullOrEmpty(result.Role.ToString())) role = result.Role.ToString() ?? role ?? string.Empty;
                if (!string.IsNullOrEmpty(result.FinishReason.ToString())) finishReason = result.FinishReason.ToString() ?? finishReason ?? string.Empty;
                var content = string.Empty;
                var base64Image = string.Empty;

                foreach (var update in result.ContentUpdate)
                {
                    if (string.IsNullOrEmpty(base64Image) && update.Kind == ChatMessageContentPartKind.Image) base64Image = Convert.ToBase64String(update.ImageBytes);
                    if (string.IsNullOrEmpty(content) && update.Kind == ChatMessageContentPartKind.Text) content += update.Text;
                }

                foreach (var update in result.ToolCallUpdates)
                {
                    if (string.IsNullOrEmpty(currentTool)) currentTool = update.FunctionName;
                    if (currentTool == update.FunctionName && update.FunctionArgumentsUpdate != null && update.FunctionArgumentsUpdate.ToArray().Any()) currentToolArgs += update.FunctionArgumentsUpdate.ToString() ?? string.Empty;

                    if (toolCalls.ContainsKey(currentTool)) toolCalls[currentTool] = currentToolArgs ?? string.Empty;
                    else
                    {
                        if (currentTool.ToLower() != SystemTools.Chat_Recursion.ToString().ToLower()) toolCalls.Add(currentTool, currentToolArgs ?? string.Empty);
                        else toolCalls.Add(SystemTools.Chat_Recursion.ToString().ToLower(), string.Empty);
                    }
                }
                var finalContentString = GetMessageContent(content, toolCalls);
                yield return new CompletionStreamChunk()
                {
                    Id = chunkId++,
                    Role = role?.ConvertStringToRole(),
                    User = completionRequest.ProfileOptions.User,
                    CompletionUpdate = finalContentString,
                    Base64Image = base64Image,
                    FinishReason = finishReason?.ConvertStringToFinishReason(),
                    ToolCalls = toolCalls
                };
            }
        }

        private List<ChatMessage> BuildCompletionMessages(CompletionRequest completionRequest)
        {
            var systemMessage = completionRequest.ProfileOptions.SystemMessage;
            var completionMessages = new List<ChatMessage>();

            if (!string.IsNullOrWhiteSpace(systemMessage)) completionMessages.Add(new SystemChatMessage(systemMessage));
            foreach (var message in completionRequest.Messages)
            {
                if (message.Role == Role.User)
                {
                    var contentBlocks = new List<ChatMessageContentPart>();

                    if (!string.IsNullOrWhiteSpace(message.Content)) contentBlocks.Add(ChatMessageContentPart.CreateTextPart(message.Content));

                    if (!string.IsNullOrEmpty(message.Base64Image))
                    {
                        var imageBytes = Convert.FromBase64String(message.Base64Image);
                        var mimeType = GetMimeTypeFromBase64(message.Base64Image);
                        contentBlocks.Add(ChatMessageContentPart.CreateImagePart(new BinaryData(imageBytes), mimeType));
                    }

                    completionMessages.Add(new UserChatMessage(contentBlocks));
                }
                else if (message.Role == Role.Assistant) completionMessages.Add(new AssistantChatMessage(message.Content));
            }
            return completionMessages;
        }

        private ChatCompletionOptions BuildCompletionOptions(CompletionRequest completion)
        {
            var options = new ChatCompletionOptions()
            {
                MaxOutputTokenCount = completion.ProfileOptions.MaxTokens ?? null,
                Temperature = completion.ProfileOptions.Temperature,
                TopP = completion.ProfileOptions.TopP,
                FrequencyPenalty = completion.ProfileOptions.FrequencyPenalty,
                PresencePenalty = completion.ProfileOptions.PresencePenalty,
                IncludeLogProbabilities = completion.ProfileOptions.Logprobs,
                EndUserId = completion.ProfileOptions.User,
            };

            if (completion.ProfileOptions.ResponseFormat == ResponseFormat.Json.ToString()) options.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();
            else if (completion.ProfileOptions.ResponseFormat == ResponseFormat.Text.ToString()) options.ResponseFormat = ChatResponseFormat.CreateTextFormat();

            if (options.IncludeLogProbabilities == true) options.TopLogProbabilityCount = completion.ProfileOptions.TopLogprobs;

            if (completion.ProfileOptions.Stop != null && completion.ProfileOptions.Stop.Length > 0)
            {
                foreach (var message in completion.ProfileOptions.Stop) options.StopSequences.Add(message);
            }

            if (completion.ProfileOptions.Tools != null)
                foreach (var tool in completion.ProfileOptions.Tools)
                {
                    var serializedParameters = JsonSerializer.Serialize(tool.Function.Parameters);
                    var newTool = ChatTool.CreateFunctionTool(tool.Function.Name, tool.Function.Description, BinaryData.FromString(serializedParameters));
                    options.Tools.Add(newTool);
                };

            if (completion.ProfileOptions.Tools != null && completion.ProfileOptions.Tools.Any())
            {
                if (completion.ProfileOptions.Tools.Count > 1) options.AllowParallelToolCalls = true;

                if (completion.ProfileOptions.ToolChoice == null || completion.ProfileOptions.ToolChoice == ToolExecutionRequirement.Auto.ToString()) options.ToolChoice = ChatToolChoice.CreateAutoChoice();
                else if (completion.ProfileOptions.ToolChoice == ToolExecutionRequirement.None.ToString()) options.ToolChoice = ChatToolChoice.CreateNoneChoice();
                else if (completion.ProfileOptions.ToolChoice == ToolExecutionRequirement.Required.ToString()) options.ToolChoice = ChatToolChoice.CreateRequiredChoice();
                else options.ToolChoice = ChatToolChoice.CreateFunctionChoice(completion.ProfileOptions.ToolChoice);
            }
            return options;
        }

        private string GetMessageContent(string? messageContent, Dictionary<string, string> toolCalls)
        {
            try
            {
                var content = messageContent ?? string.Empty;
                foreach (var tool in toolCalls)
                {
                    if (tool.Key.Equals(SystemTools.Chat_Recursion.ToString().ToLower())) return JsonSerializer.Deserialize<RecursiveChatSystemToolExecutionCall>(tool.Value)?.prompt_response ?? content;
                }
                return content;
            }
            catch (JsonException) { return string.Empty; }
            catch (NotSupportedException) { return string.Empty; }
            catch (ArgumentNullException) { return string.Empty; }
        }

        private string GetMimeTypeFromBase64(string base64)
        {
            byte[] imageBytes = Convert.FromBase64String(base64.Substring(0, 20));
            if (imageBytes.Length < 4) return "image/png";
            if (imageBytes.Take(4).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF })) return "image/jpeg";
            if (imageBytes.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 })) return "image/png";
            if (imageBytes.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38 })) return "image/gif";
            if (imageBytes.Take(4).SequenceEqual(new byte[] { 0x42, 0x4D })) return "image/bmp";
            return "image/png";
        }
    }
}
