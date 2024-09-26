using Azure.AI.OpenAI;
using System.ClientModel;
using OpenAI.Embeddings;
using IntelligenceHub.API.DTOs.ClientDTOs.EmbeddingDTOs;

namespace IntelligenceHub.Client
{
    // Combine with AI client (just duplicate the GetEmbeddings method, and move repeating logic to (a) private method(s)
    public class VectorEmbeddingClient
    {
        private AzureOpenAIClient _azureOpenAIClient;
        private EmbeddingClient _embeddingClient;
        private string _apiEndpoint;
        private string _apiKey;

        public VectorEmbeddingClient(string apiEndpoint, string apiKey, string embeddingModel = "text-embedding-3-small")
        {
            var endpointWithRouting = apiEndpoint;
            var resourceUri = new Uri(endpointWithRouting);
            var credential = new ApiKeyCredential(apiKey);
            _azureOpenAIClient = new AzureOpenAIClient(resourceUri, credential);
            _embeddingClient = _azureOpenAIClient.GetEmbeddingClient(embeddingModel);
        }

        public async Task<float[]?> GetEmbeddings(EmbeddingRequestBase completion)
        {
            var embeddingResponse = await _embeddingClient.GenerateEmbeddingAsync(completion.Input);
            return embeddingResponse.Value.Vector.ToArray();
        }
    }
}
