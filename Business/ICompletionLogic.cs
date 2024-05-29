using Azure.AI.OpenAI;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response;

namespace OpenAICustomFunctionCallingAPI.Business
{
    public interface ICompletionLogic
    {
        Task<StreamingResponse<StreamingChatCompletionsUpdate>> StreamCompletion(ChatRequestDTO completionRequest);
        string GetStreamAuthor(StreamingChatCompletionsUpdate chunk, ChatRequestDTO chatDTO);
        Task<ChatResponseDTO> ProcessCompletion(ChatRequestDTO completionRequest);
        Task<List<HttpResponseMessage>> ExecuteTools(Guid? conversationId, List<ResponseToolDTO> tools, bool streaming);
    }
}
