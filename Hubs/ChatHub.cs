using OpenAICustomFunctionCallingAPI.Business;
using Microsoft.AspNetCore.SignalR;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using Azure.AI.OpenAI;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response;

namespace OpenAICustomFunctionCallingAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ICompletionLogic _completionLogic;

        public ChatHub(ICompletionLogic completionLogic)
        {
            _completionLogic = completionLogic;
        }

        public async Task Send(string? profileName, Guid? conversationId, string? username, string? message)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                profileName = "Musician_AI_Assistant_Orchestration";
            }

            // Properties can be passed by client, or by settings/hardcoded to
            // prevent users from changing request details
            var chatDTO = new ChatRequestDTO()
            {
                ProfileName = profileName,
                Completion = message,
                ConversationId = conversationId ?? Guid.NewGuid(),
                Modifiers = new BaseCompletionDTO()
                {
                    User = username ?? "Unknown",
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
                    author = "tool";
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

            // execute any tool calls that were returned in the completion
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