using Nest;

namespace OpenAICustomFunctionCallingAPI.API.DTOs
{
    public class ChatRequestDTO
    {
        public Guid? ConversationId { get; set; }
        // should this be nullable? Maybe set a default prompt?
        public string Completion { get; set; } // = "Introduce yourself according to the details in your system message."
        public CompletionBaseDTO? Modifiers { get; set; }
    }
}
