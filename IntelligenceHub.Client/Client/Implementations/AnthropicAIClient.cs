using IntelligenceHub.API.DTOs;
using IntelligenceHub.Client.Interfaces;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using static IntelligenceHub.Common.GlobalVariables;
using Microsoft.Extensions.Options;
using IntelligenceHub.Common.Config;

namespace IntelligenceHub.Client.Implementations
{
    public class AnthropicAIClient : IAGIClient
    {
        private enum JsonStrings 
        {
            user_id,
            stop_sequence,
            max_tokens,
        }

        private readonly AnthropicClient _anthropicClient;

        public AnthropicAIClient(IOptionsMonitor<AGIClientSettings> settings, IHttpClientFactory policyFactory)
        {
            var policyClient = policyFactory.CreateClient(ClientPolicies.CompletionClient.ToString());

            var service = settings.CurrentValue.AnthropicServices.Find(service => service.Endpoint == policyClient.BaseAddress?.ToString())
                ?? throw new InvalidOperationException("service key failed to be retrieved when attempting to generate a completion.");

            var apiKey = service.Key;
            _anthropicClient = new AnthropicClient(apiKey, policyClient);
        }

        public async Task<CompletionResponse> PostCompletion(CompletionRequest completionRequest)
        {
            var request = BuildCompletionParameters(completionRequest);
            var response = await _anthropicClient.Messages.GetClaudeMessageAsync(request);

            var messages = completionRequest.Messages;
            messages.Add(ConvertFromAnthropicMessage(response));
            return new CompletionResponse
            {
                Messages = messages,
                ToolCalls = ConvertResponseTools(response.ToolCalls),
                FinishReason = ConvertFinishReason(response.StopReason, response.ToolCalls.Any())
            };
        }

        public async IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest)
        {
            var request = BuildCompletionParameters(completionRequest);
            var response = _anthropicClient.Messages.StreamClaudeMessageAsync(request);

            var toolCalls = new Dictionary<string, string>();
            await foreach (var chunk in response)
            {
                var content = string.Empty;
                var base64Image = string.Empty;
                foreach (var contentPart in chunk.Content)
                {
                    if (contentPart is ImageContent imageContent) base64Image = imageContent.Source.Data;
                    else if (contentPart is TextContent textContent) content += textContent.Text;
                }

                yield return new CompletionStreamChunk
                {
                    Base64Image = base64Image,
                    CompletionUpdate = content,
                    ToolCalls = ConvertResponseTools(chunk.ToolCalls),
                    Role = ConvertFromAnthropicRole(chunk.Role),
                    FinishReason = ConvertFinishReason(chunk.StopReason, chunk.ToolCalls.Any())
                };
            }
        }

        private MessageParameters BuildCompletionParameters(CompletionRequest request)
        {
            var anthropicMessages = new List<Anthropic.SDK.Messaging.Message>();
            var systemMessages = new List<SystemMessage>();
            foreach (var message in request.Messages)
            {
                var messageContents = ConvertToAnthropicMessage(message);
                if (message.Role == Role.System) systemMessages.Add(new Anthropic.SDK.Messaging.SystemMessage(message.Content));
                else anthropicMessages.Add(new Anthropic.SDK.Messaging.Message { Content = messageContents, Role = ConvertToAnthropicRole(message.Role) });
            }

            var anthropicTools = new List<Anthropic.SDK.Messaging.Tool>();
            foreach (var tool in request.ProfileOptions.Tools)
            {
                var schema = ConvertToolParameters(tool);
                var anthropicTool = new Anthropic.SDK.Messaging.Tool()
                {
                    Name = tool.Function.Name,
                    Description = tool.Function.Description,
                    InputSchema = schema,
                };
                anthropicTools.Add(anthropicTool);
            }

            ToolChoiceType? toolChoiceType = null;
            if (request.ProfileOptions.Tool_Choice?.ToString().ToLower() == ToolExecutionRequirement.Auto.ToString().ToLower()) toolChoiceType = ToolChoiceType.Auto;
            else if (request.ProfileOptions.Tool_Choice?.ToString().ToLower() == ToolExecutionRequirement.Required.ToString().ToLower()) toolChoiceType = ToolChoiceType.Tool;
            var messageParams = new MessageParameters()
            {
                Messages = anthropicMessages,
                Model = request.ProfileOptions.Model,
                Stream = false,
                StopSequences = request.ProfileOptions.Stop,
                System = systemMessages,
                Tools = (IList<Anthropic.SDK.Common.Tool>)anthropicTools,
                Metadata = new Dictionary<string, string> { { JsonStrings.user_id.ToString(), request.ProfileOptions.User ?? string.Empty } },
            };

            if (request.ProfileOptions.Max_Tokens.HasValue) messageParams.MaxTokens = request.ProfileOptions.Max_Tokens.Value;
            if (request.ProfileOptions.Temperature.HasValue) messageParams.Temperature = (decimal?)request.ProfileOptions.Temperature.Value;
            if (request.ProfileOptions.Top_P.HasValue) messageParams.TopP = (decimal?)request.ProfileOptions.Top_P.Value;
            if (toolChoiceType.HasValue) messageParams.ToolChoice = new ToolChoice() { Name = request.ProfileOptions.Tool_Choice, Type = (ToolChoiceType)toolChoiceType };
            return messageParams;
        }

        private List<ContentBase> ConvertToAnthropicMessage(API.DTOs.Message message)
        {
            var content = new List<ContentBase>();
            if (!string.IsNullOrEmpty(message.Content)) content.Add(new TextContent { Text = message.Content });  
            if (!string.IsNullOrEmpty(message.Base64Image)) content.Add(new ImageContent { Source = new ImageSource() { Data = message.Base64Image } });
            return content;
        }

        private IntelligenceHub.API.DTOs.Message ConvertFromAnthropicMessage(MessageResponse message)
        {
            var contentString = message.ContentBlock?.Text ?? string.Empty;
            return new API.DTOs.Message
            {
                Content = contentString,
                Role = ConvertFromAnthropicRole(message.Role)
            };
        }

        private RoleType ConvertToAnthropicRole(Role? role)
        {
            var anthropicRole = RoleType.Assistant;
            if (role == Role.User) anthropicRole = RoleType.User;
            else if (role == Role.Tool) anthropicRole = RoleType.Assistant;
            else if (role == Role.Assistant) anthropicRole = RoleType.Assistant;
            return anthropicRole;
        }

        private Role ConvertFromAnthropicRole(RoleType role)
        {
            var intelligenceHubRole = Role.Assistant;
            if (role == RoleType.User) intelligenceHubRole = Role.User;
            else if (role == RoleType.Assistant) intelligenceHubRole = Role.Assistant;
            return intelligenceHubRole;
        }

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

        private Dictionary<string, string> ConvertResponseTools(List<Anthropic.SDK.Common.Function> toolCalls)
        {
            var intelligenceHubTools = new Dictionary<string, string>();
            foreach (var tool in toolCalls) intelligenceHubTools.Add(tool.Name, tool.Arguments.ToJsonString());
            return intelligenceHubTools;
        }

        private FinishReason ConvertFinishReason(string anthropicStopReason, bool hasTools)
        {
            var reason = FinishReason.Stop;
            if (hasTools) reason = FinishReason.ToolCalls;
            else if (anthropicStopReason.ToLower() == JsonStrings.stop_sequence.ToString().ToLower()) reason = FinishReason.Stop;
            else if (anthropicStopReason.ToLower() == JsonStrings.max_tokens.ToString().ToLower()) reason = FinishReason.Length;
            return reason;
        }
    }
}
