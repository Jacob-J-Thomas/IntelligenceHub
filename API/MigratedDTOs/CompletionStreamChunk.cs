using IntelligenceHub.API.MigratedDTOs;
using static IntelligenceHub.Common.GlobalVariables;

namespace OpenAICustomFunctionCallingAPI.API.MigratedDTOs
{
    public class CompletionStreamChunk
    {
        public int Id { get; set; }
        public string CompletionUpdate { get; set; } = string.Empty;
        public Role? Role { get; set; }
        public FinishReason? FinishReason { get; set; }
        public Dictionary<string, string> ToolCalls { get; set; } = new Dictionary<string, string>();
        public List<HttpResponseMessage> ToolExecutionResponses { get; set; } = new List<HttpResponseMessage>();
        
    }
}
