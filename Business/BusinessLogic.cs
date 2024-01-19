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

        public async Task<string> GetCompletion(InboundRequest body, bool requireFunctionCall)
        {
            AIClient AIClient = new AIClient(_settings.OpenAIEndpoint, _settings.OpenAIKey, _settings.OpenAIModel);
            var functionDef = new InputRouting(body.FunctionNames);
            var sysMessage = _settings.DefaultSysMessage;

            var attempts = 0;
            var maxAttempts = 3;
            while (attempts < maxAttempts)
            {
                var completionResponse = await AIClient.Post(body.Prompt, sysMessage, functionDef);
                if (completionResponse["choices"][0]["finish_reason"].ToString() == "tool_calls")
                {
                    var completionToolCalls = completionResponse["choices"][0]["message"]["tool_calls"];
                    var successfulCalls = await CallCompletionFunctions(body, (JArray)completionToolCalls);

                    return string.Join(", ", successfulCalls);
                }
                else if (requireFunctionCall)
                {
                    attempts++;
                    Console.WriteLine($"Completion did not call an existing function. Reattempting {attempts - maxAttempts} more times...");
                }
                else
                {
                    return completionResponse["choices"][0]["message"]["content"].ToString();
                }
            }
            throw new Exception("Completion request failed to generate function call after 3 attempts for a request requiring a call.");
        }

        public async Task<List<string>> CallCompletionFunctions(InboundRequest body, JArray completionToolCalls)
        {
            var toolCallFunctions = new List<string>();
            for (int i = 0; i < completionToolCalls.Count; i++)
            {
                var arguments = completionToolCalls[i]["function"]["arguments"].ToString();
                var parsedArguments = JToken.Parse(arguments);
                var functionName = parsedArguments["functionName"].ToString();
                toolCallFunctions.Add(functionName);
            }

            // uncomment below to use the functionCall
            //foreach (var function in toolCallFunctions)
            //{
            //    // move below to a new client
            //    try
            //    {
            //        FunctionCallClient functionClient = new FunctionCallClient(body.FunctionEndpoint);
            //        if (body.FunctionNames.Keys.Contains(function))// validate response and rerequest if necessary
            //        {
            //            await functionClient.CallFunction(body.Prompt, function); // remove await to improve speed
            //            toolCallFunctions.Add(function);
            //        }
            //        else
            //        {
            //            throw new Exception($"Function does not exist: {function}");
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }
            //}
            return toolCallFunctions;
        }
    }
}
