using OpenAICustomFunctionCallingAPI.Client.DTOs.OpenAI;

namespace OpenAICustomFunctionCallingAPI.Client.OpenAI.DTOs
{
    public class OpenAIRequest
    {
        public string Model { get; set; }
        public List<Tool> Tools { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();
    }
}
