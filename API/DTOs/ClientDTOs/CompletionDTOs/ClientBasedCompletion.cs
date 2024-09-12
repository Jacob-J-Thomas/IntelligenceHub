using IntelligenceHub.API.DTOs.ClientDTOs.AICompletionDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs;
using IntelligenceHub.Controllers.DTOs;

namespace IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs
{
    public class ClientBasedCompletion : DefaultCompletionDTO
    {
        public RagRequestData RagData { get; set; }
        public string? ProfileName { get; set; }

        public ClientBasedCompletion() { }
        public ClientBasedCompletion(Profile completion) : base(completion) { }
        public ClientBasedCompletion(Profile completion, BaseCompletionDTO? modifiers) : base(completion, modifiers) { }
    }
}
