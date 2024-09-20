using IntelligenceHub.API.MigratedDTOs;

namespace IntelligenceHub.API.MigratedDTOs
{
    public class CompletionResponse
    {
        //public List<ToolCall>
        public List<Message> Messages { get; set; }
        public List<HttpResponseMessage> ToolExecutionResponses { get; set; }
        public string FinishReason { get; set; }
    }
}
