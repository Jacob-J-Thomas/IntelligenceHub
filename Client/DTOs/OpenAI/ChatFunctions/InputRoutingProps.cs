namespace OpenAICustomFunctionCallingAPI.Client.DTOs.OpenAI.ChatFunctions
{
    public class InputRoutingProps
    {
        public Property FunctionName { get; set; }
        public InputRoutingProps(string propDescription) 
        {
            FunctionName = new Property() { Type = "string", Description = propDescription };
        }
    }
}
