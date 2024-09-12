using Nest;
using Newtonsoft.Json.Linq;
using IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs;
using IntelligenceHub.Common.Attributes;
using IntelligenceHub.API.MigratedDTOs;

namespace IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs
{
    [TableName("MessageHistory")]
    public class DbMessageDTO
    {
        public int? Id { get; set; }
        public Guid? ConversationId {  get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public DateTime TimeStamp {  get; set; }
        public string Content { get; set; }
        public string ToolsCalled { get; set; }

        public DbMessageDTO() { }

        public DbMessageDTO(Message userMessage)
        {
            var name = "";
            if (userMessage.ProfileModifiers != null && userMessage.ProfileModifiers.User != null)
            {
                name = userMessage.ProfileModifiers.User;
            }
            ConvertToDbMessageDTO(userMessage.ConversationId, "user", name, userMessage.Completion);
        }

        public void ConvertToDbMessageDTO(Guid? conversationId, string role, string name, string completion)
        {
            ConversationId = conversationId;
            Role = role;
            Name = name;
            Content = completion;
            ToolsCalled = null;
            TimeStamp = DateTime.UtcNow;
        }
    }
}
