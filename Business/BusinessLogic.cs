using OpenAICustomFunctionCallingAPI.Client;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using Newtonsoft.Json.Linq;
using OpenAICustomFunctionCallingAPI.Host.Config;
using OpenAICustomFunctionCallingAPI.Client.DTOs.OpenAI.ChatFunctions;

namespace OpenAICustomFunctionCallingAPI.Business
{
    public class BusinessLogic
    {
        private readonly Settings _settings;

        public BusinessLogic(Settings settings) 
        {
            _settings = settings;
        }

        public async Task<string> BuildRoutingRequest(InboundRequest body)
        {
            AIClient AIClient = new AIClient(_settings.OpenAIEndpoint, _settings.OpenAIKey, _settings.OpenAIModel);
            var functionDef = new InputRouting(body.FunctionNames);
            var sysMessage = _settings.DefaultSysMessage;

            var attempts = 0;
            while (attempts < 3)
            {
                var completionResponse = await AIClient.Post(body.Prompt, sysMessage, functionDef);
                if (completionResponse["choices"][0]["finish_reason"].ToString() == "tool_calls")
                {
                    var completionArguments = completionResponse["choices"][0]["message"]["tool_calls"][0]["function"]["arguments"].ToString();
                    var functionName = JObject.Parse(completionArguments)["functionName"].ToString();

                    // validate response and rerequest if necessary
                    if (body.FunctionNames.Keys.Contains(functionName))
                    {
                        FunctionCallClient functionClient = new FunctionCallClient(body.FunctionEndpoint);
                        functionClient.CallFunction(body.Prompt, functionName); // not awaited to improve speed
                        return $"function_called: {functionName}";
                    }
                    else
                    {
                        attempts++;
                    }
                }
                else
                {
                    return completionResponse["choices"][0]["message"]["content"].ToString();
                }
            }
            return "function_called: failed";
        }
    }
}
