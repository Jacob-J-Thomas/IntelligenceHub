using Newtonsoft.Json.Linq;
using IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs;
using IntelligenceHub.Common.Attributes;
using IntelligenceHub.API.MigratedDTOs;

namespace IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs
{
    [TableName("MessageHistory")]
    public class DbMessage
    {
        public int? Id { get; set; }
        public Guid? ConversationId {  get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public DateTime TimeStamp {  get; set; }
        public string Content { get; set; }
        public string ToolsCalled { get; set; }
    }
}
