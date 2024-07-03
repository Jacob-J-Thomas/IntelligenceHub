namespace OpenAICustomFunctionCallingAPI.API.DTOs.Hub
{
    //string? profileName, Guid? conversationId, string? username, string? message)//, int? maxMessageHistory)//, string? database, string? ragTarget, int? maxRagDocs)
    public class StreamRequest
    {
        public string ProfileName { get; set; }
        public Guid? ConversationId { get; set; }
        public string Username { get; set; }
        public string Message { get; set; }
        public int? MaxMessageHistory { get; set; } = 5;
        public string? Database { get; set; }
        public string? RagTarget { get; set; }
        public int? MaxRagDocs { get; set; } = 5;
    }
}
