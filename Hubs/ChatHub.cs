using OpenAICustomFunctionCallingAPI.Business;
using Microsoft.AspNetCore.SignalR;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using Azure.AI.OpenAI;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs;

namespace OpenAICustomFunctionCallingAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ICompletionLogic _completionLogic;

        public ChatHub(ICompletionLogic completionLogic)
        {
            _completionLogic = completionLogic;
        }

        public async Task Send(string? profileName, Guid? conversationId, string? username, string? message, int? maxMessageHistory, string? database, string? ragTarget, int? maxRagDocs)
        {
            var chatDTO = new ChatRequestDTO();
            chatDTO.BuildStreamRequest(profileName, conversationId, username, message, maxMessageHistory, database, ragTarget, maxRagDocs);
            var response = await _completionLogic.StreamCompletion(chatDTO);

            // process the chunks returned from the completion request
            ResponseToolDTO tool = null;
            await foreach (var chunk in response)
            {
                var completionUpdate = chunk.ContentUpdate;
                var author = _completionLogic.GetStreamAuthor(chunk, chatDTO);
                if (chunk.ToolCallUpdate is StreamingFunctionToolCallUpdate toolCall)
                {
                    if (tool is null)
                    {
                        tool = new ResponseToolDTO();
                        tool.BuildFromStream(toolCall);
                        author = tool.Function.Name;
                    }
                    if (toolCall.ArgumentsUpdate is not null)
                    {
                        tool.Function.Arguments += toolCall.ArgumentsUpdate;
                        completionUpdate = toolCall.ArgumentsUpdate;
                    }
                }
                author = chunk.AuthorName ?? author; // chunk.AuthorName can supposedly be assigned to via instructions in the system prompt
                await Clients.Caller.SendAsync("broadcastMessage", author, completionUpdate);
            }

            // if tools were in the completion, execute them
            if (tool is not null)
            {
                var toolList = new List<ResponseToolDTO>();
                toolList.Add(tool);
                var functionResponse = await _completionLogic.ExecuteTools(conversationId, toolList, streaming: true);
                await Clients.Caller.SendAsync("broadcastMessage", tool.Function.Name, functionResponse);
            }
        }
    }
}