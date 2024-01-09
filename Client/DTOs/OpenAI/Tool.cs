namespace OpenAICustomFunctionCallingAPI.Client.DTOs.OpenAI
{
    public class Tool
    {
        public string Type { get; set; }
        public object Function { get; set; }
        public Tool(object functionDef) 
        {
            Type = "function";
            Function = functionDef;
        }
    }
}
