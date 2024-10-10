using IntelligenceHub.Common.Attributes;

namespace IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs
{
    [TableName("MessageHistory")]
    public class DbMessage
    {
        public int Id { get; set; }
        public Guid ConversationId {  get; set; }
        public string Role { get; set; } = string.Empty;
        public string? Base64Image { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
