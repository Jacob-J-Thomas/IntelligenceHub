﻿using IntelligenceHub.Business;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.API.MigratedDTOs;
using IntelligenceHub.Common.Exceptions;
using IntelligenceHub.Common.Handlers;
using Azure;
using Newtonsoft.Json;
using System.Text;

namespace IntelligenceHub.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ICompletionLogic _completionLogic;
        private readonly ProfileAndToolValidationHandler _validationLogic = new ProfileAndToolValidationHandler();

        public ChatHub(ICompletionLogic completionLogic)
        {
            _completionLogic = completionLogic;
        }

        public async Task Send(CompletionRequest completionRequest)
        {
            var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
            if (!string.IsNullOrEmpty(errorMessage)) throw new IntelligenceHubException(400, errorMessage);
            var response = _completionLogic.StreamCompletion(completionRequest);
            await foreach (var chunk in response) await Clients.Caller.SendAsync("broadcastMessage", chunk);
        }
    }
}