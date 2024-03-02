using System.Net.Http.Headers;
using System.Text;
using Azure.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.DAL;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;

namespace OpenAICustomFunctionCallingAPI.Client
{
    public class AIClient
    {
        private string _openAIEndpoint;
        private string _openAIKey;

        public AIClient(string openAIEndpoint, string openAIKey) 
        {
            _openAIEndpoint = openAIEndpoint;
            _openAIKey = openAIKey;
        }

        public async Task<JObject> Post(CompletionBaseDTO completion)
        {

            var retryPolicy = GetRetryPolicy();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIKey);

            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            var json = JsonConvert.SerializeObject(completion, settings);
            var content =  new StringContent(json, Encoding.UTF8, "application/json");

            using (client)
            {
                return await retryPolicy.ExecuteAsync(async () =>
                {
                    var response = await client.PostAsync(_openAIEndpoint, content);

                    var responseString = await response.Content.ReadAsStringAsync();
                    var completionResponse = JObject.Parse(responseString);

                    return completionResponse;
                });
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
    }
}
