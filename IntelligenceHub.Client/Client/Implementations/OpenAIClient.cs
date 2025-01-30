using IntelligenceHub.API.DTOs;
using IntelligenceHub.Client.Interfaces;

namespace IntelligenceHub.Client.Implementations
{
    public class OpenAIClient : IAGIClient
    {
        public Task<CompletionResponse> PostCompletion(CompletionRequest completionRequest)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest)
        {
            throw new NotImplementedException();
        }
    }
}
