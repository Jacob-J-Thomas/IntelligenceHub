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

        public ChatRequestDTO() { }

        public ChatRequestDTO(string profileName, Guid? conversationId, string username, string message)
        {
            BuildStreamRequest(profileName, conversationId, username, message);
        }

        public void BuildStreamRequest(string profileName, Guid? conversationId, string username, string message)
        {
            ProfileName = profileName;
            Completion = message;
            ConversationId = conversationId ?? Guid.NewGuid();
            Modifiers = new BaseCompletionDTO()
            {
                User = username ?? "Unknown",
                Stream = true
            };
        }
    }
}
