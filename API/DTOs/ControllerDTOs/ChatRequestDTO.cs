using Nest;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs
{
    public class ChatRequestDTO
    {
        public Guid? ConversationId { get; set; }
        public string ProfileName { get; set; }
        public string Completion { get; set; } // = "Introduce yourself according to the details in your system message."
        public BaseCompletionDTO? Modifiers { get; set; }
    }
}
