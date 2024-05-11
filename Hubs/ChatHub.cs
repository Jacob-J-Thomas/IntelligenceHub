using System.Threading.Tasks;
using OpenAICustomFunctionCallingAPI.Business;
using Microsoft.AspNetCore.SignalR;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using Azure;

namespace OpenAICustomFunctionCallingAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ICompletionLogic _completionLogic;

        public ChatHub(ICompletionLogic completionLogic)
        {
            _completionLogic = completionLogic;
        }

        public async Task Send(string username, string message)//, Guid? ConversationId)
        {
            var conversationId = Guid.NewGuid();
            // Properties can be passed by client, or by settings/hardcoded to
            // prevent users from changing request details
            var chatDTO = new ChatRequestDTO()
            {
                ProfileName = "Musician_AI_Assistant_Orchestration",
                Completion = message,
                ConversationId = conversationId
            };

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
                    author = username ?? chunk.Role.ToString();
                }

                author = chunk.AuthorName ?? author; // chunk.AuthorName can supposedly be assigned to via instructions in the system prompt

                await Clients.Caller.SendAsync("broadcastMessage", author, chunk.ContentUpdate);
            };
        }
    }
}