using OpenAI.Chat;
using IntelligenceHub.API.MigratedDTOs;

namespace IntelligenceHub.API.MigratedDTOs
{
    public class CompletionRequest
    {
        public Guid? ConversationId { get; set; }
        public string Profile { get; set; }
        public Profile ProfileOptions { get; set; } = new Profile();
        public List<Message> Messages { get; set; }
    }
}
