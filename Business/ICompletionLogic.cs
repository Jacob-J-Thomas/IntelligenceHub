using Azure.AI.OpenAI;
using OpenAICustomFunctionCallingAPI.API.DTOs;

namespace OpenAICustomFunctionCallingAPI.Business
{
    public interface ICompletionLogic
    {
        Task<StreamingResponse<StreamingChatCompletionsUpdate>> StreamCompletion(ChatRequestDTO completionRequest, string username);
        Task<ChatResponseDTO> ProcessCompletion(ChatRequestDTO completionRequest);
    }
}
