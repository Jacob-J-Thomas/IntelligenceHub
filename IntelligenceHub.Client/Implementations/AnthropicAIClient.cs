using IntelligenceHub.API.DTOs;
using IntelligenceHub.Client.Interfaces;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using static IntelligenceHub.Common.GlobalVariables;
using Microsoft.Extensions.Options;
using IntelligenceHub.Common.Config;
using IntelligenceHub.API.DTOs.Tools;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;

namespace IntelligenceHub.Client.Implementations
{
    /// <summary>
    /// A client for interacting with Claude's API.
    /// </summary>
    public class AnthropicAIClient : IAGIClient
    {
        private enum AnthropicSpecificStrings 
        {
            user_id,
            stop_sequence,
            max_tokens,
            end_turn
        }

        private readonly AnthropicClient _anthropicClient;

        /// <summary>
        /// Creates a new instance of the AnthropicAIClient.
        /// </summary>
        /// <param name="settings">The AGIClient settings used to configure this client.</param>
        /// <param name="policyFactory">The client factory used to retrieve a policy.</param>
        /// <exception cref="InvalidOperationException">Thrown if the provided settings are invalid.</exception>
        public AnthropicAIClient(IOptionsMonitor<AGIClientSettings> settings, IHttpClientFactory policyFactory)
        {
            var policyClient = policyFactory.CreateClient(ClientPolicies.AnthropicAIClientPolicy.ToString());

            var service = settings.CurrentValue.AnthropicServices.Find(service => service.Endpoint == policyClient.BaseAddress?.ToString())
                ?? throw new InvalidOperationException("service key failed to be retrieved when attempting to generate a completion.");

            var apiKey = service.Key;
            _anthropicClient = new AnthropicClient(apiKey, policyClient);
        }

        /// <summary>
        /// Generates an image based on the provided prompt. Is not supported by Anthropic.
        /// </summary>
        /// <param name="prompt">The prompt used to generate the image.</param>
        /// <returns>The base 64 representation of the image, or null if an error was encountered.</returns>
        public Task<string?> GenerateImage(string prompt)
        {
            // Anthropic does not currently support image gen
            return Task.FromResult<string?>(null);
        }

        /// <summary>
        /// Generates a completion based on the provided request.
        /// </summary>
        /// <param name="completionRequest">The request details used to generate the completion.</param>
        /// <returns>A completion response.</returns>
        public async Task<CompletionResponse> PostCompletion(CompletionRequest completionRequest)
        {
            try
            {
                var request = BuildCompletionParameters(completionRequest);
                var response = await _anthropicClient.Messages.GetClaudeMessageAsync(request);

                var toolCalls = ConvertResponseTools(response.ToolCalls);

                var responseContent = response.ContentBlock?.Text ?? string.Empty;
                if (response.Content != null) responseContent = string.Join("", response.Content.OfType<TextContent>().Select(tc => tc.Text));
                var contentString = GetMessageContent(responseContent, toolCalls);

                var messages = completionRequest.Messages;
                var responseMessage = ConvertFromAnthropicMessage(response, contentString);
                messages.Add(responseMessage);
                return new CompletionResponse
                {
                    Messages = messages,
                    ToolCalls = toolCalls,
                    FinishReason = ConvertFinishReason(response.StopReason, response.ToolCalls.Any())
                };
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return new CompletionResponse() { FinishReason = FinishReasons.TooManyRequests };
            }
        }

        /// <summary>
        /// Streams a completion based on the provided request.
        /// </summary>
        /// <param name="completionRequest">The request details used to generate the completion.</param>
        /// <returns>Any asyncronous collection of streaming chunks containing the completion response.</returns>
        public async IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest)
        {
            const int maxAttempts = 5;
            var attempt = 0;
            var rng = new Random();
            var alreadySent = new StringBuilder(); // for de-dup if a retry restarts content

            while (true)
            {
                attempt++;

                var request = BuildCompletionParameters(completionRequest, true);
                var stream = _anthropicClient.Messages.StreamClaudeMessageAsync(request);

                await using var e = stream.GetAsyncEnumerator();

                while (true)
                {
                    bool hasItem;

                    try
                    {
                        hasItem = await e.MoveNextAsync(); // <-- exceptions happen here mid-stream
                    }
                    catch (Exception ex) when (IsTransientAnthropicStreamError(ex) && attempt < maxAttempts)
                    {
                        // Backoff + retry new attempt
                        var delayMs = (int)(Math.Pow(2, attempt - 1) * 500) + rng.Next(0, 250);
                        await Task.Delay(delayMs);
                        break; // break inner while, continue outer while (new attempt)
                    }

                    if (!hasItem) yield break; // stream completed cleanly

                    var chunk = e.Current;

                    string content = "";
                    string base64Image = "";

                    if (chunk.Content != null)
                    {
                        foreach (var part in chunk.Content)
                        {
                            if (part is ImageContent img) base64Image = img.Source.Data;
                            else if (part is TextContent txt) content += txt.Text;
                        }
                    }
                    else if (!string.IsNullOrEmpty(chunk.Delta?.Text))
                    {
                        foreach (var c in chunk.Delta.Text) content += c;
                    }

                    // De-duplication if a retry restarted the stream
                    if (!string.IsNullOrEmpty(content) && alreadySent.Length > 0)
                    {
                        var sent = alreadySent.ToString();
                        if (content.StartsWith(sent, StringComparison.Ordinal)) content = content.Substring(sent.Length);
                        else if (sent.StartsWith(content, StringComparison.Ordinal)) content = ""; // entirely duplicate
                    }

                    if (!string.IsNullOrEmpty(content)) alreadySent.Append(content);

                    var contentString = GetMessageContent(content, new Dictionary<string, string>());

                    yield return new CompletionStreamChunk
                    {
                        Base64Image = base64Image,
                        CompletionUpdate = contentString,
                        ToolCalls = ConvertResponseTools(chunk.ToolCalls ?? new List<Anthropic.SDK.Common.Function>()),
                        Role = Role.Assistant,
                        FinishReason = ConvertFinishReason(chunk.StopReason, chunk.ToolCalls?.Any() == true)
                    };
                }
            }
        }

        /// <summary>
        /// Assists with retry logic by identifying transient errors during streaming.
        /// </summary>
        /// <param name="ex">The exception being checked.</param>
        /// <returns>A bool indicating if the error is transient or not.</returns>
        private static bool IsTransientAnthropicStreamError(Exception ex)
        {
            // Cover likely mid-stream cases:
            if (ex is TaskCanceledException || ex is TimeoutException) return true;
            if (ex is IOException) return true;

            if (ex is HttpRequestException hre)
            {
                // .StatusCode is nullable
                if (hre.StatusCode.HasValue)
                {
                    var code = (int)hre.StatusCode.Value;
                    if (code == 429 || code == 408 || (code >= 500 && code <= 599) || code == 529)
                        return true;
                }
                // Some SDKs don’t populate StatusCode on mid-stream breaks; fall through to message
            }

            // Anthropic SDK may throw custom exceptions with "overloaded" in the message/type
            var msg = ex.ToString();
            if (msg.Contains("overloaded", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("overload", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("529", StringComparison.Ordinal))
                return true;

            return false;
        }

        /// <summary>
        /// Builds the parameters used to generate a completion.
        /// </summary>
        /// <param name="request">The completion request details.</param>
        /// <returns>The parameters used to generate a completion.</returns>
        private MessageParameters BuildCompletionParameters(CompletionRequest request, bool stream = false)
        {
            var anthropicMessages = new List<Anthropic.SDK.Messaging.Message>();
            var systemMessages = new List<SystemMessage>();
            foreach (var message in request.Messages)
            {
                var messageContents = ConvertToAnthropicMessage(message);
                var mimeType = string.Empty;
                if (!string.IsNullOrEmpty(message.Base64Image)) mimeType = GetMimeTypeFromBase64(message.Base64Image);

                if (message.Role == Role.System) systemMessages.Add(new Anthropic.SDK.Messaging.SystemMessage(message.Content));
                if (message.Base64Image != null) anthropicMessages.Add(new Anthropic.SDK.Messaging.Message { Content = new List<ContentBase> { new ImageContent { Source = new ImageSource() { Data = message.Base64Image, MediaType = mimeType } } }, Role = ConvertToAnthropicRole(message.Role) });
                else if (!string.IsNullOrEmpty(message.Content)) anthropicMessages.Add(new Anthropic.SDK.Messaging.Message { Content = messageContents, Role = ConvertToAnthropicRole(message.Role) });
            }

            var anthropicTools = new List<Anthropic.SDK.Common.Tool>();
            if (request.ProfileOptions != null && request.ProfileOptions.Tools != null) foreach (var tool in request.ProfileOptions?.Tools)
            {
                var schema = ConvertToolParameters(tool);
                var serializedParams = JsonSerializer.Serialize(tool.Function.Parameters);
                var nodefiedParams = JsonNode.Parse(serializedParams);

                var function = new Anthropic.SDK.Common.Function(tool.Function.Name, tool.Function.Description, nodefiedParams);
                var anthropicTool = new Anthropic.SDK.Common.Tool(function);
                anthropicTools.Add(anthropicTool);
            }

            if (!ValidAnthropicModels.TryGetValue(request.ProfileOptions?.Model ?? string.Empty, out int contextLimit)) contextLimit = 8192;

            ToolChoiceType? toolChoiceType = null;
            if (request.ProfileOptions?.ToolChoice?.ToString().ToLower() == ToolExecutionRequirement.Auto.ToString().ToLower()) toolChoiceType = ToolChoiceType.Auto;
            else if (request.ProfileOptions?.ToolChoice?.ToString().ToLower() == ToolExecutionRequirement.Required.ToString().ToLower()) toolChoiceType = ToolChoiceType.Tool;
            var messageParams = new MessageParameters()
            {
                Messages = anthropicMessages,
                Model = request.ProfileOptions?.Model ?? DefaultAnthropicModel, // only one model is supported from anthropic
                Stream = stream,
                StopSequences = request.ProfileOptions?.Stop,
                System = systemMessages,
                Tools = anthropicTools,
                Metadata = new Dictionary<string, string> { { AnthropicSpecificStrings.user_id.ToString(), request.ProfileOptions?.User ?? string.Empty } },
                MaxTokens = request.ProfileOptions?.MaxTokens ?? contextLimit
            };

            if (request.ProfileOptions.MaxTokens.HasValue) messageParams.MaxTokens = request.ProfileOptions.MaxTokens.Value;
            if (request.ProfileOptions.Temperature.HasValue) messageParams.Temperature = Convert.ToDecimal(request.ProfileOptions.Temperature.Value);
            if (request.ProfileOptions.TopP.HasValue) messageParams.TopP = Convert.ToDecimal(request.ProfileOptions.TopP.Value);
            if (toolChoiceType.HasValue) messageParams.ToolChoice = new ToolChoice() { Name = request.ProfileOptions.ToolChoice, Type = (ToolChoiceType)toolChoiceType };
            return messageParams;
        }

        /// <summary>
        /// Sets the message content based on the presence of a ChatRecursion tool call.
        /// </summary>
        /// <param name="messageContent">The original message content.</param>
        /// <param name="toolCalls">The tool calls associated with the content.</param>
        /// <returns>The original messageContent, or a response to send to the next ChatRecursion LLM model.</returns>
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

        /// <summary>
        /// Converts an IntelligenceHub message to an Anthropic ContentBase object.
        /// </summary>
        /// <param name="message">The message to be converted.</param>
        /// <returns>The ContentBase object.</returns>
        private List<ContentBase> ConvertToAnthropicMessage(API.DTOs.Message message)
        {
            var content = new List<ContentBase>();
            if (!string.IsNullOrEmpty(message.Content)) content.Add(new TextContent { Text = message.Content });  
            if (!string.IsNullOrEmpty(message.Base64Image)) content.Add(new ImageContent { Source = new ImageSource() { Data = message.Base64Image } });
            return content;
        }

        /// <summary>
        /// Converts an Anthropic MessageResponse object to a Message.
        /// </summary>
        /// <param name="message">The message response object returned from the client.</param>
        /// <param name="contentString">The content of the response message.</param>
        /// <returns></returns>
        private IntelligenceHub.API.DTOs.Message ConvertFromAnthropicMessage(MessageResponse message, string contentString)
        {
            return new API.DTOs.Message
            {
                Content = contentString,
                Role = ConvertFromAnthropicRole(message.Role)
            };
        }

        /// <summary>
        /// Converts an IntelligenceHub Role to an Anthropic Role.
        /// </summary>
        /// <param name="role">The IntelligenceHub role to be converted.</param>
        /// <returns>The converted role.</returns>
        private RoleType ConvertToAnthropicRole(Role? role)
        {
            var anthropicRole = RoleType.Assistant;
            if (role == Role.User) anthropicRole = RoleType.User;
            else if (role == Role.Tool) anthropicRole = RoleType.Assistant;
            else if (role == Role.Assistant) anthropicRole = RoleType.Assistant;
            return anthropicRole;
        }

        /// <summary>
        /// Converts an Anthropic role to an IntelligenceHub role.
        /// </summary>
        /// <param name="role">The role to be conveted.</param>
        /// <returns>The converted role.</returns>
        private Role ConvertFromAnthropicRole(RoleType role)
        {
            var intelligenceHubRole = Role.Assistant;
            if (role == RoleType.User) intelligenceHubRole = Role.User;
            else if (role == RoleType.Assistant) intelligenceHubRole = Role.Assistant;
            return intelligenceHubRole;
        }

        /// <summary>
        /// Converts an IntelligenceHub tool to an Anthropic tool.
        /// </summary>
        /// <param name="tool">The tool to be converted.</param>
        /// <returns>The converted tool/InputSchema.</returns>
        private InputSchema ConvertToolParameters(IntelligenceHub.API.DTOs.Tools.Tool tool)
        {
            var anthropicProperties = new Dictionary<string, Anthropic.SDK.Messaging.Property>();
            foreach (var propertyData in tool.Function.Parameters.properties)
            {
                var property = propertyData.Value;
                var anthropicProperty = new Anthropic.SDK.Messaging.Property()
                {
                    Description = property.description,
                    Type = property.type,
                };
                anthropicProperties.Add(propertyData.Key, anthropicProperty);
            }

            return new InputSchema()
            {
                Properties = anthropicProperties,
                Required = tool.Function.Parameters.required,
                Type = tool.Type,
            };
        }

        /// <summary>
        /// Converts an Anthropic tool call to an IntelligenceHub tool call.
        /// </summary>
        /// <param name="toolCalls">The tool calls to be converted.</param>
        /// <returns>The converted tool calls.</returns>
        private Dictionary<string, string> ConvertResponseTools(List<Anthropic.SDK.Common.Function> toolCalls)
        {
            var intelligenceHubTools = new Dictionary<string, string>();
            foreach (var tool in toolCalls)
            {
                if (tool.Name.ToLower() != SystemTools.Chat_Recursion.ToString().ToLower()) intelligenceHubTools.Add(tool.Name, tool.Arguments.ToJsonString());
                else intelligenceHubTools.Add(SystemTools.Chat_Recursion.ToString().ToLower(), string.Empty);
            }
            return intelligenceHubTools;
        }

        /// <summary>
        /// Converts an Anthropic stop reason to an IntelligenceHub finish reason.
        /// </summary>
        /// <param name="anthropicStopReason">The stop reason to be converted.</param>
        /// <param name="hasTools">Whether or not tools were included in the response.</param>
        /// <returns>The converted finish reason.</returns>
        private FinishReasons ConvertFinishReason(string anthropicStopReason, bool hasTools)
        {
            var reason = FinishReasons.Stop;
            if (hasTools) reason = FinishReasons.ToolCalls;
            else if (anthropicStopReason?.ToLower() == AnthropicSpecificStrings.end_turn.ToString().ToLower()) reason = FinishReasons.Stop;
            else if (anthropicStopReason?.ToLower() == AnthropicSpecificStrings.stop_sequence.ToString().ToLower()) reason = FinishReasons.Stop;
            else if (anthropicStopReason?.ToLower() == AnthropicSpecificStrings.max_tokens.ToString().ToLower()) reason = FinishReasons.Length;
            return reason;
        }

        /// <summary>
        /// Retrieves the MIME type of the base 64 image.
        /// </summary>
        /// <param name="base64">The base64 image string.</param>
        /// <returns>The MIME type string.</returns>
        private string GetMimeTypeFromBase64(string base64)
        {
            byte[] imageBytes = Convert.FromBase64String(base64.Substring(0, 20)); // Read only the first few bytes
            if (imageBytes.Length < 4) return "image/png";

            // Check the file signature (magic number) to determine the MIME type
            if (imageBytes.Take(4).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF })) return "image/jpeg";
            if (imageBytes.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 })) return "image/png";
            if (imageBytes.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38 })) return "image/gif";
            if (imageBytes.Take(4).SequenceEqual(new byte[] { 0x42, 0x4D })) return "image/bmp";
            return "image/png"; // Default if unknown
        }
    }
}
