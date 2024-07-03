using OpenAICustomFunctionCallingAPI.Business;
using Microsoft.AspNetCore.SignalR;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using Azure.AI.OpenAI;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs;
using Nest;
using OpenAICustomFunctionCallingAPI.API.DTOs.Hub;

namespace OpenAICustomFunctionCallingAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ICompletionLogic _completionLogic;

        public ChatHub(ICompletionLogic completionLogic)
        {
            _completionLogic = completionLogic;
        }

        public async Task Send(StreamRequest request)
        {
            var chatDTO = new ChatRequestDTO();
            chatDTO.BuildStreamRequest(request.ProfileName, request.ConversationId, request.Username, request.Message, request.MaxMessageHistory, request.Database, request.RagTarget, request.MaxRagDocs);
            var response = await _completionLogic.StreamCompletion(chatDTO);

            // process the chunks returned from the completion request
            ResponseToolDTO tool = null;
            await foreach (var chunk in response)
            {
                var completionUpdate = chunk.ContentUpdate;
                var author = _completionLogic.GetStreamAuthor(chunk, chatDTO.ProfileName, chatDTO.ProfileModifiers.User);
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
                var functionResponse = await _completionLogic.ExecuteTools(request.ConversationId, toolList, streaming: true);
                await Clients.Caller.SendAsync("broadcastMessage", tool.Function.Name, functionResponse);
            }
        }

        public async Task ExecuteClientBasedCompletion(ClientBasedCompletion completionRequest)
        {
            var chatDTO = new ChatRequestDTO();
            chatDTO.BuildStreamRequest(completionRequest.Model, null, completionRequest.User, null, null, completionRequest.RagData.RagDatabase, completionRequest.RagData.RagTarget, completionRequest.RagData.MaxRagDocs);
            var response = await _completionLogic.StreamCompletion(chatDTO);

            // process the chunks returned from the completion request
            ResponseToolDTO tool = null;
            await foreach (var chunk in response)
            {
                var completionUpdate = chunk.ContentUpdate;
                var author = _completionLogic.GetStreamAuthor(chunk, chatDTO.ProfileName, chatDTO.ProfileModifiers.User);
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
                var functionResponse = await _completionLogic.ExecuteTools(conversationId: null, toolList, streaming: true);
                await Clients.Caller.SendAsync("broadcastMessage", tool.Function.Name, functionResponse);
            }
        }
    }
}