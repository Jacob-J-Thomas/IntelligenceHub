using Azure.AI.OpenAI;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs.Response;

namespace IntelligenceHub.Business
{
    public interface ICompletionLogic
    {
        Task<StreamingResponse<StreamingChatCompletionsUpdate>> StreamCompletion(ChatRequestDTO completionRequest);
        string GetStreamAuthor(StreamingChatCompletionsUpdate chunk, string profileName, string user = "user");
        Task<ChatResponseDTO> ProcessCompletion(ChatRequestDTO completionRequest);
        Task<List<HttpResponseMessage>> ExecuteTools(Guid? conversationId, List<ResponseToolDTO> tools, bool streaming);
    }
}
