using Azure.AI.OpenAI;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response;

namespace OpenAICustomFunctionCallingAPI.Business
{
    public interface ICompletionLogic
    {
        Task<StreamingResponse<StreamingChatCompletionsUpdate>> StreamCompletion(ChatRequestDTO completionRequest, string username);
        Task<ChatResponseDTO> ProcessCompletion(ChatRequestDTO completionRequest);
        Task<List<HttpResponseMessage>> ExecuteStreamTools(Guid? conversationId, string username, List<ResponseToolDTO> tools);
    }
}
