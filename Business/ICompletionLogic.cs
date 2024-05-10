using OpenAICustomFunctionCallingAPI.API.DTOs;

namespace OpenAICustomFunctionCallingAPI.Business
{
    public interface ICompletionLogic
    {
        Task<ChatResponseDTO> ProcessCompletionRequest(ChatRequestDTO request);
    }
}
