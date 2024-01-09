namespace OpenAICustomFunctionCallingAPI.Controllers.DTOs
{
    public class InboundRequest
    {
        public string Prompt { get; set; }
        public string FunctionEndpoint { get; set; } // The API route to which you would like function calls to be made to
        public Dictionary<string, string> FunctionNames { get; set; } // This list of valid functions to call
    }
}
