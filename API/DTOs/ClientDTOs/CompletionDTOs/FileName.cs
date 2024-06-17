using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.MessageDTOs;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs
{
    public class ClientBasedCompletion : DefaultCompletionDTO
    {
        public RagRequestData RagData { get; set; }

        public ClientBasedCompletion() { }
        public ClientBasedCompletion(APIProfileDTO completion) : base(completion) { }
        public ClientBasedCompletion(APIProfileDTO completion, BaseCompletionDTO? modifiers) : base(completion, modifiers) { }
    }
}
