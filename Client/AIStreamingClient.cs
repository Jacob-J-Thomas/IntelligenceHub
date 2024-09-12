//using System.Net.Http.Headers;
//using System.Text;
//using Azure.Core;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using Newtonsoft.Json.Serialization;
//using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
//using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
//using OpenAICustomFunctionCallingAPI.DAL;
//using OpenAICustomFunctionCallingAPI.DAL.DTOs;
//using Polly;
//using Polly.Contrib.WaitAndRetry;
//using Polly.Retry;
//using Azure.AI.OpenAI;
//using Azure;
//using System.Text.Json;
//using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs;
//using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response;

//namespace OpenAICustomFunctionCallingAPI.Client
//{
//    public class AIStreamingClient
//    {
//        private OpenAIClient _streamingClient;
//        private string _apiEndpoint;
//        private string _apiKey;

//        public AIStreamingClient(string ApiEndpoint, string ApiKey) 
//        {
//            _streamingClient = new OpenAIClient(ApiKey);
//            _apiEndpoint = ApiEndpoint + "chat/completions";
//            _apiKey = ApiKey;
//        }

//        // currently this only supports openAI's API (and azure OpenAI could easily be added), and therefore implements their client via the official
//        // package supported by Microsoft... not even sure if others offer streaming at the moment
//        public async Task<StreamingResponse<StreamingChatCompletionsUpdate>> StreamCompletion(DefaultCompletionDTO completion)
//        {
//            var chatOptions = BuildChatOptions(completion);

//            try
//            {
//                return await _streamingClient.GetChatCompletionsStreamingAsync(chatOptions);
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//        }

//        public ChatCompletionsOptions BuildChatOptions(DefaultCompletionDTO completion)
//        {
//            var chatOptions = new ChatCompletionsOptions();
//            chatOptions.DeploymentName = completion.Model ?? null;
//            chatOptions.EnableLogProbabilities = completion.Logprobs ?? null;
//            chatOptions.LogProbabilitiesPerToken = completion.Top_Logprobs ?? null;
//            chatOptions.NucleusSamplingFactor = completion.Top_P ?? null;
//            chatOptions.Temperature = completion.Temperature ?? null;
//            chatOptions.FrequencyPenalty = completion.Frequency_Penalty ?? null;
//            chatOptions.PresencePenalty = completion.Presence_Penalty ?? null;
//            chatOptions.MaxTokens = completion.Max_Tokens ?? null;
//            chatOptions.ChoiceCount = completion.N ?? null;
//            chatOptions.Seed = completion.Seed ?? null;
//            chatOptions.User = completion.User ?? null;
//            //TokenSelectionBiases = completion.Logit_Bias ?? null;

//            if (completion.Tool_Choice != null && (completion.Tool_Choice == "none" || completion.Tool_Choice == "auto"))
//            {
//                var definition = new FunctionDefinition();
//                definition.Name = completion.Tool_Choice;
//                chatOptions.ToolChoice = definition;
//            }

//            if (completion.Response_Format == "json")
//            {
//                chatOptions.ResponseFormat = ChatCompletionsResponseFormat.JsonObject;
//            }
//            else if (completion.Response_Format == "text")
//            {
//                chatOptions.ResponseFormat = ChatCompletionsResponseFormat.Text;
//            }

//            if (completion.Tools != null && completion.Tools.Count > 0)
//                foreach (var tool in completion.Tools)
//                {
//                    var definition = new FunctionDefinition()
//                    {
//                        Name = tool.Function.Name,
//                        Description = tool.Function.Description,
//                        Parameters = BinaryData.FromObjectAsJson<ParametersDTO>(tool.Function.Parameters)
//                    };
//                    chatOptions.Tools.Add(new ChatCompletionsFunctionToolDefinition(definition));
//                }

//            foreach (var message in completion.Messages)
//            {
//                if (message.Role == "user")
//                {
//                    chatOptions.Messages.Add(new ChatRequestUserMessage(message.Content));
//                }
//                else if (message.Role == "assistant")
//                {
//                    chatOptions.Messages.Add(new ChatRequestAssistantMessage(message.Content));
//                }
//                else if (message.Role == "system")
//                {
//                    chatOptions.Messages.Add(new ChatRequestSystemMessage(message.Content));
//                }
//                else if (message.Role == "tool")
//                {
//                    chatOptions.Messages.Add(new ChatRequestToolMessage(message.Content, message.ToolCallID));
//                }
//                // these will likely be useful later
//                //else if (message.Role = "")
//                //{
//                //    chatOptions.Messages.Add(new ChatMessageTextContentItem()); // probably for RAG documents
//                //}
//                //else if (message.Role = "")
//                //{
//                //    chatOptions.Messages.Add(new ChatMessageImageContentItem());
//                //}
//                //else if (message.Role = "")
//                //{
//                //    chatOptions.Messages.Add(new ChatFinishDetails());
//                //}
//            }

//            if (completion.Stop != null && completion.Stop.Length > 0)
//            {
//                foreach (var message in completion.Stop)
//                {
//                    chatOptions.StopSequences.Add(message);
//                }
//            }

//            // checks if the system message is null or if it was already included (due to being part of the conversation history)
//            if (completion.System_Message != null && chatOptions.Messages.Contains(new ChatRequestSystemMessage(completion.System_Message)))
//            {
//                chatOptions.Messages.Add(new ChatRequestSystemMessage(completion.System_Message));
//            }
//            return chatOptions;
//        }
//    }
//}
