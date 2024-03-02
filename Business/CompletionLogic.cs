using OpenAICustomFunctionCallingAPI.Client;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using Newtonsoft.Json.Linq;
using OpenAICustomFunctionCallingAPI.Host.Config;
//using OpenAICustomFunctionCallingAPI.DAL;
using Azure;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OpenAICustomFunctionCallingAPI.DAL;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using System.Net;
using System.Linq;

namespace OpenAICustomFunctionCallingAPI.Business
{
    public class CompletionLogic
    {
        private readonly IConfiguration _configuration;
        private readonly AIClient _AIClient;
        private readonly ProfileRepository _profileRepository;
        private readonly List<HttpStatusCode> _serverSideErrorCodes;

        public CompletionLogic(Settings settings) 
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _AIClient = new AIClient(settings.OpenAIEndpoint, settings.OpenAIKey);
            _profileRepository = new ProfileRepository(settings.DbConnectionString);
            _serverSideErrorCodes = new List<HttpStatusCode>() 
            { 
                HttpStatusCode.BadGateway,
                HttpStatusCode.GatewayTimeout,
                HttpStatusCode.HttpVersionNotSupported,
                HttpStatusCode.InsufficientStorage,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.LoopDetected,
                HttpStatusCode.NetworkAuthenticationRequired,
                HttpStatusCode.NotExtended,
                HttpStatusCode.NotImplemented,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.VariantAlsoNegotiates,
            };
        }

        // move to an AIClient in API layer
        public async Task<string> GetCompletion(string profileName, ChatRequestDTO completionRequest)//, bool requireFunctionCall)
        {
            

            var completionDto = await BuildCompletionDTO(profileName, completionRequest);

            // chose which AIClient to build and execute with here

            var attempts = 0;
            var maxAttempts = 5;
            while (attempts < maxAttempts)
            {
                try
                {
                    var completionResponse = await _AIClient.Post(completionDto);
                    //ensure success status code

                    if (completionResponse["choices"][0]["finish_reason"] == null)
                    {
                        return null; // handle errors better maybe?
                    }

                    if (completionResponse["choices"][0]["finish_reason"].ToString() == "tool_calls")
                    {
                        var completionToolCalls = completionResponse["choices"][0]["message"]["tool_calls"];
                        var successfulCalls = await ExecuteTools(profileName, (JArray)completionToolCalls);

                        return string.Join(", ", successfulCalls);
                    }
                    //else if (requireFunctionCall)
                    //{
                    //    attempts++;
                    //    Console.WriteLine($"Completion did not call an existing function. Reattempting {attempts - maxAttempts} more times...");
                    //}
                    else
                    {
                        return completionResponse["choices"][0]["message"]["content"].ToString();
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode != null && _serverSideErrorCodes.Contains((HttpStatusCode)ex.StatusCode))
                    {
                        // add logic to switch to fail safe services
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            throw new Exception("Completion request failed to generate function call after 3 attempts for a request requiring a call.");
        }

        public async Task<CompletionBaseDTO> BuildCompletionDTO(string profileName, ChatRequestDTO chatRequest)
        {
            var openAIDbProfile = await _profileRepository.GetByNameWithToolsAsync(profileName);
            var openAIRequest = new CompletionBaseDTO();
            if (chatRequest.Modifiers != null)
            {
                openAIRequest = new CompletionBaseDTO(openAIDbProfile, chatRequest.Modifiers);
            }
            else
            {
                openAIRequest = new CompletionBaseDTO(openAIDbProfile);
            }
            openAIRequest.Messages = AddMessages(chatRequest.Completion, openAIRequest.System_Message, chatRequest.ConversationId);
            return openAIRequest;
        }

        public List<Message> AddMessages(string completion, string? systemMessage, Guid? conversationId)
        {
            var messages = new List<Message>();
            if (systemMessage != null)
            {
                messages.Add(new Message("system", systemMessage));
            }
            if (completion != null)
            {
                messages.Add(new Message("user", completion));
            }
            if (conversationId != null)
            {
                // Maybe move into seperate method "GetMessageHistory"?
                //var messageHistory = await _messageHistoryRepository.GetAsync(conversationId)
                // foreach (var message in messageHistory) { messages.Add(message) };
            }
            
            return messages;
        }

        // Move this to an AIClient in API layer? - maybe not
        // Break this into two different functions
        public async Task<Dictionary<string, string>> ExecuteTools(string profileName, JArray completionToolCalls)
        {
            var profile = await _profileRepository.GetByNameWithToolsAsync(profileName);
            var tools = profile.Tools;
            var toolList = new List<string>();
            if (tools != null && tools.Count > 0)
            {
                foreach (var tool in tools)
                {
                    toolList.Add(tool.Function.Name);
                }
            }
            
            var toolsToCall = new Dictionary<string, string>();
            var profilesToCall = new Dictionary<string, string>();
            for (int i = 0; i < completionToolCalls.Count; i++)
            {
                var arguments = completionToolCalls[i]["function"]["arguments"].ToString();
                var parsedArguments = JToken.Parse(arguments);
                var actionName = parsedArguments["functionName"].ToString();

                if (toolList.Contains(actionName))
                {
                    toolsToCall.Add(actionName, "put properties here");
                }
                if (profile.Reference_Profiles != null && profile.Reference_Profiles.Contains(actionName))
                { 
                    //actions.Add(actionName, original prompt or some kind of property prompt?);
                }
            }

            //var errorMessage = await CallExternalToolClient()
            //var errorMessage = await CallReferenceModels(
            // if (errorMessage != null)
            // log error and relevant data from actions somehow
            
            // combine actions and return data
            Dictionary<string, string> allActions = toolsToCall.Concat(profilesToCall).ToDictionary(pair => pair.Key, pair => pair.Value);

            return allActions;
        }
    }
}
