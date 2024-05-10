using Nest;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs
{
    public class ChatResponseDTO
    {
        public Guid ConversationId { get; set; }
        public string Completion { get; set; } 
        public List<HttpResponseMessage> ToolResponses { get; set; } = new List<HttpResponseMessage>();
    }
}
