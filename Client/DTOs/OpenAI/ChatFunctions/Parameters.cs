namespace OpenAICustomFunctionCallingAPI.Client.DTOs.OpenAI.ChatFunctions
{
    public class Parameters
    {
        public string Type { get; set; }
        public object Properties { get; set; }
        public List<string> Required {  get; set; }
        public Parameters(object properties, List<string> required)
        {
            Type = "object";
            Properties = properties;
            Required = required;
        }
    }
}
