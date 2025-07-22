using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL.Tenant;
using IntelligenceHub.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace IntelligenceHub.Tests.Unit.Hubs
{
    public class ChatHubTests
    {
        private static ChatHub CreateHub(
            Mock<ICompletionLogic> completionMock,
            Mock<IValidationHandler> validationMock,
            Mock<IUserLogic> userLogicMock,
            Mock<ITenantProvider> tenantProviderMock,
            Mock<IUsageService> usageMock,
            Mock<ISingleClientProxy> clientProxy,
            string? apiKey = "key")
        {
            var clients = new Mock<IHubCallerClients>();
            clients.Setup(c => c.Caller).Returns(clientProxy.Object);
            var context = new Mock<HubCallerContext>();
            var httpContext = new DefaultHttpContext();
            if (apiKey != null)
            {
                httpContext.Request.Headers["X-Api-Key"] = apiKey;
            }
            context.Setup(c => c.GetHttpContext()).Returns(httpContext);

            var hub = new ChatHub(completionMock.Object, validationMock.Object, userLogicMock.Object, tenantProviderMock.Object, usageMock.Object)
            {
                Clients = clients.Object,
                Context = context.Object
            };

            return hub;
        }

        private static async IAsyncEnumerable<APIResponseWrapper<CompletionStreamChunk>> GetStream(params APIResponseWrapper<CompletionStreamChunk>[] chunks)
        {
            foreach (var chunk in chunks)
            {
                yield return chunk;
                await Task.Yield();
            }
        }

        [Fact]
        public async Task Send_SendsValidationError_WhenValidationFails()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var userLogicMock = new Mock<IUserLogic>();
            var tenantProviderMock = new Mock<ITenantProvider>();
            var usageMock = new Mock<IUsageService>();
            var clientProxy = new Mock<ISingleClientProxy>();
            tenantProviderMock.SetupAllProperties();
            usageMock.Setup(u => u.ValidateAndIncrementUsageAsync(It.IsAny<DbUser>())).ReturnsAsync(APIResponseWrapper<bool>.Success(true));
            userLogicMock.Setup(u => u.GetUserByApiTokenAsync(It.IsAny<string>())).ReturnsAsync(new DbUser { TenantId = Guid.NewGuid() });
            var hub = CreateHub(completionMock, validationMock, userLogicMock, tenantProviderMock, usageMock, clientProxy);
            var request = new CompletionRequest();
            const string error = "Invalid";
            validationMock.Setup(v => v.ValidateChatRequest(request)).Returns(error);

            await hub.Send(request);

            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => (string)o[0] == error), default), Times.Once);
            completionMock.Verify(c => c.StreamCompletion(It.IsAny<CompletionRequest>()), Times.Never);
        }

        [Fact]
        public async Task Send_StreamsCompletionChunks_WhenRequestValid()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var userLogicMock = new Mock<IUserLogic>();
            var tenantProviderMock = new Mock<ITenantProvider>();
            var usageMock = new Mock<IUsageService>();
            var clientProxy = new Mock<ISingleClientProxy>();
            tenantProviderMock.SetupAllProperties();
            usageMock.Setup(u => u.ValidateAndIncrementUsageAsync(It.IsAny<DbUser>())).ReturnsAsync(APIResponseWrapper<bool>.Success(true));
            userLogicMock.Setup(u => u.GetUserByApiTokenAsync(It.IsAny<string>())).ReturnsAsync(new DbUser { TenantId = Guid.NewGuid() });
            var hub = CreateHub(completionMock, validationMock, userLogicMock, tenantProviderMock, usageMock, clientProxy);
            var request = new CompletionRequest();
            var chunk = APIResponseWrapper<CompletionStreamChunk>.Success(new CompletionStreamChunk { CompletionUpdate = "hi" });
            completionMock.Setup(c => c.StreamCompletion(request)).Returns(GetStream(chunk));

            await hub.Send(request);

            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => ((CompletionStreamChunk)o[0]).CompletionUpdate == "hi"), default), Times.Once);
        }

        [Fact]
        public async Task Send_SendsErrorMessage_WhenChunkIndicatesFailure()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var userLogicMock = new Mock<IUserLogic>();
            var tenantProviderMock = new Mock<ITenantProvider>();
            var usageMock = new Mock<IUsageService>();
            var clientProxy = new Mock<ISingleClientProxy>();
            tenantProviderMock.SetupAllProperties();
            usageMock.Setup(u => u.ValidateAndIncrementUsageAsync(It.IsAny<DbUser>())).ReturnsAsync(APIResponseWrapper<bool>.Success(true));
            userLogicMock.Setup(u => u.GetUserByApiTokenAsync(It.IsAny<string>())).ReturnsAsync(new DbUser { TenantId = Guid.NewGuid() });
            var hub = CreateHub(completionMock, validationMock, userLogicMock, tenantProviderMock, usageMock, clientProxy);
            var request = new CompletionRequest();
            const string msg = "missing";
            var failure = APIResponseWrapper<CompletionStreamChunk>.Failure(msg, GlobalVariables.APIResponseStatusCodes.NotFound);
            completionMock.Setup(c => c.StreamCompletion(request)).Returns(GetStream(failure));

            await hub.Send(request);

            var expected = $"Response Status: {GlobalVariables.APIResponseStatusCodes.NotFound}. Error message: {msg}";
            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => (string)o[0] == expected), default), Times.Once);
        }

        [Fact]
        public async Task Send_SendsDefaultError_OnException()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var userLogicMock = new Mock<IUserLogic>();
            var tenantProviderMock = new Mock<ITenantProvider>();
            var usageMock = new Mock<IUsageService>();
            var clientProxy = new Mock<ISingleClientProxy>();
            tenantProviderMock.SetupAllProperties();
            usageMock.Setup(u => u.ValidateAndIncrementUsageAsync(It.IsAny<DbUser>())).ReturnsAsync(APIResponseWrapper<bool>.Success(true));
            userLogicMock.Setup(u => u.GetUserByApiTokenAsync(It.IsAny<string>())).ReturnsAsync(new DbUser { TenantId = Guid.NewGuid() });
            var hub = CreateHub(completionMock, validationMock, userLogicMock, tenantProviderMock, usageMock, clientProxy);
            var request = new CompletionRequest();
            completionMock.Setup(c => c.StreamCompletion(request)).Throws(new System.Exception());

            await hub.Send(request);

            var expected = $"Response Status: {500}. Error message: {GlobalVariables.DefaultExceptionMessage}";
            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => (string)o[0] == expected), default), Times.Once);
        }

        [Fact]
        public async Task Send_ReturnsError_WhenUsageLimitExceeded()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var userLogicMock = new Mock<IUserLogic>();
            var tenantProviderMock = new Mock<ITenantProvider>();
            var usageMock = new Mock<IUsageService>();
            var clientProxy = new Mock<ISingleClientProxy>();
            tenantProviderMock.SetupAllProperties();
            usageMock.Setup(u => u.ValidateAndIncrementUsageAsync(It.IsAny<DbUser>()))
                     .ReturnsAsync(APIResponseWrapper<bool>.Failure("limit", GlobalVariables.APIResponseStatusCodes.TooManyRequests));
            userLogicMock.Setup(u => u.GetUserByApiTokenAsync(It.IsAny<string>())).ReturnsAsync(new DbUser { TenantId = Guid.NewGuid() });
            var hub = CreateHub(completionMock, validationMock, userLogicMock, tenantProviderMock, usageMock, clientProxy);
            var request = new CompletionRequest();

            await hub.Send(request);

            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => ((string)o[0]).Contains("limit")), default), Times.Once);
        }

        [Fact]
        public async Task Send_ReturnsError_WhenUserIsNull()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var userLogicMock = new Mock<IUserLogic>();
            var tenantProviderMock = new Mock<ITenantProvider>();
            var usageMock = new Mock<IUsageService>();
            var clientProxy = new Mock<ISingleClientProxy>();
            tenantProviderMock.SetupAllProperties();
            usageMock.Setup(u => u.ValidateAndIncrementUsageAsync(It.IsAny<DbUser>())).ReturnsAsync(APIResponseWrapper<bool>.Success(true));
            userLogicMock.Setup(u => u.GetUserByApiTokenAsync(It.IsAny<string>())).ReturnsAsync((DbUser?)null);
            var hub = CreateHub(completionMock, validationMock, userLogicMock, tenantProviderMock, usageMock, clientProxy);
            var request = new CompletionRequest();

            await hub.Send(request);

            var expected = $"Response Status: {500}. Error message: {GlobalVariables.DefaultExceptionMessage}";
            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => (string)o[0] == expected), default), Times.Once);
        }

        [Fact]
        public async Task Send_ReturnsError_WhenApiKeyMissing()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var userLogicMock = new Mock<IUserLogic>();
            var tenantProviderMock = new Mock<ITenantProvider>();
            var usageMock = new Mock<IUsageService>();
            var clientProxy = new Mock<ISingleClientProxy>();
            tenantProviderMock.SetupAllProperties();
            var hub = CreateHub(completionMock, validationMock, userLogicMock, tenantProviderMock, usageMock, clientProxy, null);
            var request = new CompletionRequest();

            await hub.Send(request);

            var expected = $"Response Status: {500}. Error message: {GlobalVariables.DefaultExceptionMessage}";
            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => (string)o[0] == expected), default), Times.Once);
            userLogicMock.Verify(u => u.GetUserByApiTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Send_SendsErrorMessage_ForTooManyRequestsStatus()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var userLogicMock = new Mock<IUserLogic>();
            var tenantProviderMock = new Mock<ITenantProvider>();
            var usageMock = new Mock<IUsageService>();
            var clientProxy = new Mock<ISingleClientProxy>();
            tenantProviderMock.SetupAllProperties();
            usageMock.Setup(u => u.ValidateAndIncrementUsageAsync(It.IsAny<DbUser>())).ReturnsAsync(APIResponseWrapper<bool>.Success(true));
            userLogicMock.Setup(u => u.GetUserByApiTokenAsync(It.IsAny<string>())).ReturnsAsync(new DbUser { TenantId = Guid.NewGuid() });
            var hub = CreateHub(completionMock, validationMock, userLogicMock, tenantProviderMock, usageMock, clientProxy);
            var request = new CompletionRequest();
            const string msg = "limit";
            var failure = APIResponseWrapper<CompletionStreamChunk>.Failure(msg, GlobalVariables.APIResponseStatusCodes.TooManyRequests);
            completionMock.Setup(c => c.StreamCompletion(request)).Returns(GetStream(failure));

            await hub.Send(request);

            var expected = $"Response Status: {GlobalVariables.APIResponseStatusCodes.TooManyRequests}. Error message: {msg}";
            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => (string)o[0] == expected), default), Times.Once);
        }

        [Fact]
        public async Task Send_SendsErrorMessage_ForInternalErrorStatus()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var userLogicMock = new Mock<IUserLogic>();
            var tenantProviderMock = new Mock<ITenantProvider>();
            var usageMock = new Mock<IUsageService>();
            var clientProxy = new Mock<ISingleClientProxy>();
            tenantProviderMock.SetupAllProperties();
            usageMock.Setup(u => u.ValidateAndIncrementUsageAsync(It.IsAny<DbUser>())).ReturnsAsync(APIResponseWrapper<bool>.Success(true));
            userLogicMock.Setup(u => u.GetUserByApiTokenAsync(It.IsAny<string>())).ReturnsAsync(new DbUser { TenantId = Guid.NewGuid() });
            var hub = CreateHub(completionMock, validationMock, userLogicMock, tenantProviderMock, usageMock, clientProxy);
            var request = new CompletionRequest();
            const string msg = "error";
            var failure = APIResponseWrapper<CompletionStreamChunk>.Failure(msg, GlobalVariables.APIResponseStatusCodes.InternalError);
            completionMock.Setup(c => c.StreamCompletion(request)).Returns(GetStream(failure));

            await hub.Send(request);

            var expected = $"Response Status: {GlobalVariables.APIResponseStatusCodes.InternalError}. Error message: {msg}";
            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => (string)o[0] == expected), default), Times.Once);
        }
    }
}