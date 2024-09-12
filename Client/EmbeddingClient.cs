using System.Net.Http.Headers;
using System.Text;
using Azure.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using IntelligenceHub.API.DTOs.ClientDTOs.AICompletionDTOs;
using IntelligenceHub.Controllers.DTOs;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.DTOs;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using Azure.AI.OpenAI;
using Azure;
using System.Text.Json;
using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs.Response;
using IntelligenceHub.API.DTOs.ClientDTOs.EmbeddingDTOs;

namespace IntelligenceHub.Client
{
    // Combine with AI client (just duplicate the GetEmbeddings method, and move repeating logic to a private method)S
    public class EmbeddingClient
    {
        private OpenAIClient _streamingClient;
        private string _apiEndpoint;
        private string _apiKey;

        public EmbeddingClient(string ApiEndpoint, string ApiKey) 
        {
            _streamingClient = new OpenAIClient(ApiKey);
            _apiEndpoint = ApiEndpoint + "embeddings";
            _apiKey = ApiKey;
        }

        public async Task<EmbeddingResponse> GetEmbeddings(EmbeddingRequestBase completion)
        {
            EmbeddingRequestBase requestDTO = (EmbeddingRequestBase)completion;
            var retryPolicy = GetRetryPolicy();
            JsonSerializerSettings settings = new()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.Auto // Add this to ensure derived type properties are serialized
            };

            HttpClient client = new();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var json = JsonConvert.SerializeObject(requestDTO, typeof(EmbeddingRequestBase), settings);
            var content =  new StringContent(json, Encoding.UTF8, "application/json");
            using (client) return await retryPolicy.ExecuteAsync(async () => await ProcessRequest(client, content));
        }

        private async Task<EmbeddingResponse> ProcessRequest(HttpClient client, StringContent content)
        {
            var httpResponse = await client.PostAsync(_apiEndpoint, content);
            httpResponse.EnsureSuccessStatusCode();
            var responseString = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<EmbeddingResponse>(responseString);
        }

        private AsyncRetryPolicy GetRetryPolicy()
        {
            var delay = Backoff
               .DecorrelatedJitterBackoffV2(
               medianFirstRetryDelay: TimeSpan.FromSeconds(1),
               retryCount: 5);

            return Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(delay);
        }
    }
}
