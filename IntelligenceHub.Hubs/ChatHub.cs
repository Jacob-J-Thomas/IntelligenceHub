using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using IntelligenceHub.DAL.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Hubs
{
    /// <summary>
    /// A SignalR hub used to stream chat completions to the client
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ICompletionLogic _completionLogic;
        private readonly IValidationHandler _validationLogic;
        private readonly IUserLogic _userLogic;
        private readonly ITenantProvider _tenantProvider;
        private readonly IUsageService _usageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatHub"/> class.
        /// </summary>
        /// <param name="completionLogic">The completion logic used to process the request</param>
        /// <param name="validationHandler">The validation logic used to ensure the request body is valid</param>
        public ChatHub(ICompletionLogic completionLogic, IValidationHandler validationHandler, IUserLogic userLogic, ITenantProvider tenantProvider, IUsageService usageService)
        {
            _completionLogic = completionLogic;
            _validationLogic = validationHandler;
            _userLogic = userLogic;
            _tenantProvider = tenantProvider;
            _usageService = usageService;
        }

        /// <summary>
        /// Sends a completion request to the server, and responds with the completion results.
        /// </summary>
        /// <param name="completionRequest">The completion request's body</param>
        /// <returns>An awaitable task after sending the completion response chunks back to the client</returns>
        public async Task Send(CompletionRequest completionRequest)
        {
            try
            {
                var tenantClaim = Context.User?.FindFirst(TenantIdClaim)?.Value;
                if (tenantClaim is null || !Guid.TryParse(tenantClaim, out var tenantId))
                {
                    await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {500}. Error message: {DefaultExceptionMessage}");
                    return;
                }

                var user = await _userLogic.GetUserByTenantIdAsync(tenantId);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {500}. Error message: {DefaultExceptionMessage}");
                    return;
                }

                _tenantProvider.TenantId = tenantId;
                _tenantProvider.User = user;

                var usageResult = await _usageService.ValidateAndIncrementUsageAsync(user);
                if (!usageResult.IsSuccess)
                {
                    await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {APIResponseStatusCodes.TooManyRequests}. Error message: {usageResult.ErrorMessage}");
                    return;
                }

                var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    await Clients.Caller.SendAsync("broadcastMessage", errorMessage);
                    return;
                }

                var response = _completionLogic.StreamCompletion(completionRequest);
                await foreach (var chunk in response)
                {
                    if (chunk.IsSuccess) await Clients.Caller.SendAsync("broadcastMessage", chunk.Data);
                    else if (chunk.StatusCode == APIResponseStatusCodes.NotFound) await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {chunk.StatusCode}. Error message: {chunk.ErrorMessage}");
                    else if (chunk.StatusCode == APIResponseStatusCodes.TooManyRequests) await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {chunk.StatusCode}. Error message: {chunk.ErrorMessage}");
                    else if (chunk.StatusCode == APIResponseStatusCodes.InternalError) await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {chunk.StatusCode}. Error message: {chunk.ErrorMessage}");
                    else await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {chunk.StatusCode}. Error message: {chunk.ErrorMessage}");
                }
            }
            catch (Exception)
            {
                await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {500}. Error message: {DefaultExceptionMessage}");
            }
        }
    }
}