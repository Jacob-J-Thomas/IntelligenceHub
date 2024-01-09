namespace OpenAICustomFunctionCallingAPI.Client.DTOs.FunctionCalling
{
    public class OutgoingFunctionCall
    {
        public string Prompt { get; set; }
        public string FunctionName { get; set; }

        public OutgoingFunctionCall(string prompt, string functionName)
        {
            Prompt = prompt;
            FunctionName = functionName;
        }
    }
}
