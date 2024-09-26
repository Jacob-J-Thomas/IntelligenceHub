namespace OpenAICustomFunctionCallingAPI.API.MigratedDTOs.ToolDTOs
{
    public class ToolExecutionCall
    {
        public string ToolName { get; set; } = string.Empty;
        public string? Arguments { get; set; }
    }
}
