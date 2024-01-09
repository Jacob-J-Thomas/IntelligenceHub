using Newtonsoft.Json;

namespace OpenAICustomFunctionCallingAPI.Client.DTOs.OpenAI.ChatFunctions
{
    public class InputRouting
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Parameters Parameters { get; set; }
        public InputRouting(Dictionary<string, string> validFunctionNames)
        {
            Name = "input_routing";
            Description = "Returns a string (functionName) which is the name of a function that will be attached to an API call. Ultimately this will be used to generate additional completions, or call functions from more specialized " +
                "OpenAI model configurations.";

            var functionNamesJson = JsonConvert.SerializeObject(validFunctionNames).ToString();
            var propDescription = "The name of the function that will be called to get a response from a more specialized OpenAI model configuration. The only acceptable values for the string are in the below json key value pairs " +
                "delimited by triple backticks. The keys in the json represent valid names of the possible functions to call, while the values are descriptors of what the functions do. Returning any value that does not exist in the" +
                "list will result in an error." +
                "\n```\n" +
                functionNamesJson +
                "\n```";

            var properties = new InputRoutingProps(propDescription);
            var requiredProps = new List<string>() { "functionName" };
            Parameters = new Parameters(properties, requiredProps);
        }
    }
}
