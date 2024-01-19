using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenAICustomFunctionCallingAPI.Client.DTOs.OpenAI;
using OpenAICustomFunctionCallingAPI.Client.OpenAI.DTOs;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;

namespace OpenAICustomFunctionCallingAPI.Client
{
    public class AIClient
    {
        private string _openAIEndpoint;
        private string _openAIKey;
        private string _openAIModel;

        public AIClient(string openAIEndpoint, string openAIKey, string openAIModel) 
        {
            _openAIEndpoint = openAIEndpoint;
            _openAIKey = openAIKey;
            _openAIModel = openAIModel;
        }

        public async Task<JObject> Post(string prompt, string instructions, object toolFunction)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);

            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(delay);


            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIKey);

            var body = BuildRequestBody(prompt, instructions, toolFunction);

            using (client)
            {
                return await retryPolicy.ExecuteAsync(async () =>
                {
                    var response = await client.PostAsync(_openAIEndpoint, body);

                    var responseString = await response.Content.ReadAsStringAsync();
                    var completionResponse = JObject.Parse(responseString);

                    return completionResponse;
                });
            }
        }

        public HttpContent BuildRequestBody(string prompt, string instructions, object toolFunction)
        {
            OpenAIRequest request = new OpenAIRequest();
            request.Model = _openAIModel;
            request.Tools = new List<Tool>()
            {
                new Tool(toolFunction)
            };

            request.Messages.Add(new Message("system", instructions));
            request.Messages.Add(new Message("user", prompt));

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            var json = JsonConvert.SerializeObject(request, settings);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}
