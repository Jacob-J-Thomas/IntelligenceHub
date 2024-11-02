using IntelligenceHub.API.DTOs;

namespace IntelligenceHub.Client
{
    public interface IAGIClient
    {
        public Task<CompletionResponse> PostCompletion(CompletionRequest completionRequest);
        public IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest);
    }
}
