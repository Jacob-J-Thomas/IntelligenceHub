using Nest;
using Newtonsoft.Json.Linq;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs;
using OpenAICustomFunctionCallingAPI.Common.Attributes;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.MessageDTOs
{
    [TableName("MessageHistory")]
    public class DbMessageDTO
    {
        public string ConversationId {  get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public DateTime TimeStamp {  get; set; }
        public string Content { get; set; }
        public string ToolsCalled { get; set; }

        public DbMessageDTO() { }

        public DbMessageDTO(ChatRequestDTO userMessage)
        {
            var name = "";
            if (userMessage.Modifiers != null && userMessage.Modifiers.User != null)
            {
                name = userMessage.Modifiers.User;
            }
            ConvertToDbMessageDTO(userMessage.ConversationId, "user", name, userMessage.Completion);
        }

        public void ConvertToDbMessageDTO(Guid? conversationId, string role, string name, string completion)
        {
            if (conversationId != null)
            {
                ConversationId = conversationId.ToString();
            }
            Role = role;
            Name = name;
            Content = completion;
            ToolsCalled = null;
            TimeStamp = DateTime.UtcNow;
        }
    }
}
