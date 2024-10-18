using IntelligenceHub.Business;
using Microsoft.AspNetCore.SignalR;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Common.Exceptions;
using IntelligenceHub.Common.Handlers;
using Microsoft.AspNetCore.Authorization;

namespace IntelligenceHub.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ICompletionLogic _completionLogic;
        private readonly ProfileValidationHandler _validationLogic = new ProfileValidationHandler();

        public ChatHub(ICompletionLogic completionLogic)
        {
            _completionLogic = completionLogic;
        }

        public async Task Send(CompletionRequest completionRequest)
        {
            try
            {
                var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
                if (!string.IsNullOrEmpty(errorMessage)) throw new IntelligenceHubException(400, errorMessage);
                var response = _completionLogic.StreamCompletion(completionRequest);
                await foreach (var chunk in response) await Clients.Caller.SendAsync("broadcastMessage", chunk);
            }
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, ex.Message);
            }
        }
    }
}