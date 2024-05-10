using OpenAICustomFunctionCallingAPI.API.DTOs;

namespace OpenAICustomFunctionCallingAPI.Business
{
    public interface IValidationLogic
    {
        string ValidateChatRequest(string context, ChatRequestDTO request);
    }
}
