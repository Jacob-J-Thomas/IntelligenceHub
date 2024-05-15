using System.Net.Http.Headers;
using System.Text;
using Azure.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.DAL;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using Azure.AI.OpenAI;
using Azure;
using System.Text.Json;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs;

namespace OpenAICustomFunctionCallingAPI.Client
{
    public class AIClient
    {
        private OpenAIClient _streamingClient;
        private string _apiEndpoint;
        private string _apiKey;

        public AIClient(string ApiEndpoint, string ApiKey) 
        {
            _streamingClient = new OpenAIClient(ApiKey);
            _apiEndpoint = ApiEndpoint;
            _apiKey = ApiKey;
        }

        // currently this only supports openAI's API, and therefore implements their client via the official
        // package supported by Microsoft... not even sure if others offer streaming at the moment
        public async Task<StreamingResponse<StreamingChatCompletionsUpdate>> StreamCompletion(StandardCompletionDTO completion)
        {
            // move this logic into a method and constructor in StandardCompletionDTO
            var chatOptions = new ChatCompletionsOptions();

            if (completion.Model != null)
            {
                chatOptions.DeploymentName = completion.Model;
            }
            if (completion.N != null)
            {
                chatOptions.ChoiceCount = completion.N; // this probably should be disabled for streaming
            }
            if (completion.Logprobs != null)
            {
                chatOptions.EnableLogProbabilities = completion.Logprobs;
            }
            if (completion.Frequency_Penalty != null)
            {
                chatOptions.FrequencyPenalty = completion.Frequency_Penalty;
            }
            if (completion.Top_Logprobs != null)
            {
                chatOptions.LogProbabilitiesPerToken = completion.Top_Logprobs;
            }
            if (completion.Max_Tokens != null)
            {
                chatOptions.MaxTokens = completion.Max_Tokens;
            }
            if (completion.Top_P != null)
            {
                chatOptions.NucleusSamplingFactor = completion.Top_P;
            }
            if (completion.Presence_Penalty != null)
            {
                chatOptions.PresencePenalty = completion.Presence_Penalty;
            }
            if (completion.Seed != null)
            {
                chatOptions.Seed = completion.Seed;
            }
            if (completion.Temperature != null)
            {
                chatOptions.Temperature = completion.Temperature;
            }
            if (completion.User != null)
            {
                chatOptions.User = completion.User;
            }
            //if (completion.Logit_Bias != null)
            //{
            //    TokenSelectionBiases = completion.Logit_Bias // not implemented
            //}








            //if (completion.Tool_Choice == "auto" || (completion.Tool_Choice == null && completion.Tools.Count > 0))
            //{
            //    chatOptions. = FunctionDefinition.Auto;
            //}
            //else if (completion.Tool_Choice == "none" || completion.Tool_Choice == null || completion.Tools.Count == 0)
            //{
            //    chatOptions.ToolChoice = FunctionDefinition.None;
            //}
            if (completion.Tool_Choice != null && (completion.Tool_Choice == "none" || completion.Tool_Choice == "auto"))
            {
                var definition = new FunctionDefinition();
                definition.Name = completion.Tool_Choice;
                chatOptions.ToolChoice = definition;
            }

            if (completion.Response_Format == "json")
            {
                chatOptions.ResponseFormat = ChatCompletionsResponseFormat.JsonObject;
            }
            else if (completion.Response_Format == "text")
            {
                chatOptions.ResponseFormat = ChatCompletionsResponseFormat.Text;
            }
            // I guess only json and text are supported by Microsoft and their .net package?
            //else if (completion.Response_Format == "srt")
            //{

            //}
            //else if (completion.Response_Format == "verbose_json")
            //{

            //}
            //else if (completion.Response_Format == "vtt")
            //{

            //}

            if (completion.Tools != null && completion.Tools.Count > 0)
            foreach (var tool in completion.Tools)
            {
                var definition = new FunctionDefinition()
                {
                    Name = tool.Function.Name,
                    Description = tool.Function.Description,
                    Parameters = BinaryData.FromObjectAsJson<ParametersDTO>(tool.Function.Parameters)
                };
                chatOptions.Tools.Add(new ChatCompletionsFunctionToolDefinition(definition));
            }
            
            var messageList = new List<ChatRequestMessage>();
            foreach (var message in completion.Messages) 
            {
                if (message.Role == "user")
                {
                    chatOptions.Messages.Add(new ChatRequestUserMessage(message.Content));
                }
                else if (message.Role == "assistant")
                {
                    chatOptions.Messages.Add(new ChatRequestAssistantMessage(message.Content));
                }
                else if (message.Role == "system")
                {
                    chatOptions.Messages.Add(new ChatRequestSystemMessage(message.Content));
                }
                else if (message.Role == "tool")
                {
                    chatOptions.Messages.Add(new ChatRequestToolMessage(message.Content, message.ToolCallID));
                }
                // these will likely be useful later
                //else if (message.Role = "")
                //{
                //    chatOptions.Messages.Add(new ChatMessageTextContentItem()); // probably for RAG documents
                //}
                //else if (message.Role = "")
                //{
                //    chatOptions.Messages.Add(new ChatMessageImageContentItem());
                //}
                //else if (message.Role = "")
                //{
                //    chatOptions.Messages.Add(new ChatFinishDetails());
                //}
            }

            if (completion.Stop != null && completion.Stop.Length > 0)
            foreach (var message in completion.Stop)
            {
                chatOptions.StopSequences.Add(message);
            }

            // checks if the system message is null or if it was already included (due to being part of the conversation history)
            if (completion.System_Message != null && chatOptions.Messages.Contains(new ChatRequestSystemMessage(completion.System_Message)))
            {
                chatOptions.Messages.Add(new ChatRequestSystemMessage(completion.System_Message));
            }

            try
            {
                return await _streamingClient.GetChatCompletionsStreamingAsync(chatOptions);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public async Task<CompletionResponseDTO?> PostCompletion(StandardCompletionDTO completion)
        {
            var retryPolicy = GetRetryPolicy();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            var json = JsonConvert.SerializeObject(completion, settings);
            var content =  new StringContent(json, Encoding.UTF8, "application/json");

            using (client)
            {
                return await retryPolicy.ExecuteAsync(async () => await ProcessRequest(client, content));
            }
        }

        public AsyncRetryPolicy GetRetryPolicy()
        {
            var delay = Backoff
               .DecorrelatedJitterBackoffV2(
               medianFirstRetryDelay: TimeSpan.FromSeconds(1),
               retryCount: 5);

            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(delay);

            return retryPolicy;
        }

        public async Task<CompletionResponseDTO> ProcessRequest(HttpClient client, StringContent content)
        {
            var httpResponse = await client.PostAsync(_apiEndpoint, content);
            httpResponse.EnsureSuccessStatusCode();

            var responseString = await httpResponse.Content.ReadAsStringAsync();
            var completionResponse = JsonConvert.DeserializeObject<CompletionResponseDTO>(responseString);

            return completionResponse;
        }
    }
}
