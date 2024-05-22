using Nest;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs
{
    public class ChatRequestDTO
    {
        public Guid? ConversationId { get; set; }
        public string ProfileName { get; set; }
        public string Completion { get; set; }
        public BaseCompletionDTO? Modifiers { get; set; }
    }
}
