using System.Threading.Tasks;
using OpenAICustomFunctionCallingAPI.Business;
using Microsoft.AspNetCore.SignalR;
using OpenAICustomFunctionCallingAPI.API.DTOs;

namespace OpenAICustomFunctionCallingAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ICompletionLogic _businessLogic;

        public ChatHub(ICompletionLogic businessLogic)
        {
            _businessLogic = businessLogic;
        }

        public async Task Send(string name, string message)
        {
            // Properties can be passed by client, or by settings/hardcoded to
            // prevent users from changing request details
            var chatDTO = new ChatRequestDTO()
            {
                ProfileName = "Musician_AI_Assistant_Orchestration",
                Completion = message,
            };

            //var completion = await _businessLogic.ProcessCompletionRequest(chatDTO);
            var completion = "poopoo peepee";

            // change this to only send to original client
            await Clients.All.SendAsync("broadcastMessage", name, completion);
        }
    }
}