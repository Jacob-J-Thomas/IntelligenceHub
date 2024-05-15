using System.Threading.Tasks;
using OpenAICustomFunctionCallingAPI.Business;
using Microsoft.AspNetCore.SignalR;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using Azure;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs;
using Azure.AI.OpenAI;
using Nest;

namespace OpenAICustomFunctionCallingAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ICompletionLogic _completionLogic;

        public ChatHub(ICompletionLogic completionLogic)
        {
            _completionLogic = completionLogic;
        }

        public async Task Send(string? profileName, Guid? nullableConversationId, string? username, string? message)//, Guid? ConversationId)
        {
            var conversationId = nullableConversationId ?? Guid.NewGuid();

            // Properties can be passed by client, or by settings/hardcoded to
            // prevent users from changing request details
            var chatDTO = new ChatRequestDTO()
            {
                ProfileName = profileName ?? "Musician_AI_Assistant_Orchestration",
                Completion = message ?? "Hi, how are you today?",
                ConversationId = conversationId,
                Modifiers = new BaseCompletionDTO()
                {
                    User = username ?? "Test Account",
                }
            };

            
            var toolArguments = "";
            ResponseToolDTO tool = null;
            var response = await _completionLogic.StreamCompletion(chatDTO, username);
            await foreach (var chunk in response)
            {
                var author = chunk.AuthorName;
                if (chunk.Role == "assistant")
                {
                    author = chatDTO.ProfileName;
                }
                else if (chunk.Role == "user")
                {
                    author = username ?? "user";
                }
                else if (chunk.Role == "tool")
                {
                    // handle tools
                }

                // Return message or tool details
                if (chunk.ToolCallUpdate is StreamingFunctionToolCallUpdate toolCall)
                {
                    if (toolCall.ArgumentsUpdate != null && tool == null)
                    {
                        // create a constructor and method for this?
                        tool = new ResponseToolDTO()
                        {
                            Id = toolCall.Id,
                            Function = new ResponseFunctionDTO()
                            {
                                Name = toolCall.Name
                            }
                        };
                    }
                    tool.Function.Arguments += toolCall.ArgumentsUpdate;
                    await Clients.Caller.SendAsync("broadcastMessage", toolCall.Name, toolCall.ArgumentsUpdate);
                }
                else if (chunk.ContentUpdate != null)
                {
                    author = chunk.AuthorName ?? author; // chunk.AuthorName can supposedly be assigned to via instructions in the system prompt
                    await Clients.Caller.SendAsync("broadcastMessage", author, chunk.ContentUpdate);
                }
            }

            if (tool != null)
            {
                var toolList = new List<ResponseToolDTO>();
                toolList.Add(tool);
                var functionResponse = await _completionLogic.ExecuteStreamTools(conversationId, username, toolList);
                await Clients.Caller.SendAsync("broadcastMessage", tool.Function.Name, functionResponse);
            }
        }
    }
}