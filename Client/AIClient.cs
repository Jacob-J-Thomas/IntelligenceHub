using System.Net.Http.Headers;
using System.Text;
using Azure.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
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
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response;

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

        public async Task<CompletionResponseDTO?> PostCompletion(DefaultCompletionDTO completion)
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

        private async Task<CompletionResponseDTO> ProcessRequest(HttpClient client, StringContent content)
        {
            var httpResponse = await client.PostAsync(_apiEndpoint, content);
            httpResponse.EnsureSuccessStatusCode();

            var responseString = await httpResponse.Content.ReadAsStringAsync();
            var completionResponse = JsonConvert.DeserializeObject<CompletionResponseDTO>(responseString);

            return completionResponse;
        }

        private AsyncRetryPolicy GetRetryPolicy()
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
