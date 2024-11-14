using Castle.Components.DictionaryAdapter.Xml;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business;
using IntelligenceHub.Common.Handlers;
using IntelligenceHub.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace IntelligenceHub.Tests.Unit.Hubs
{
    public class ChatHubTests
    {
        private readonly Mock<ICompletionLogic> _completionLogicMock;
        private readonly Mock<IValidationHandler> _validationLogicMock;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly Mock<IHubClients> _hubClientsMock;  // Mocking IHubClients directly
        private readonly Mock<HubCallerContext> _hubCallerContextMock;
        private readonly ChatHub _chatHub;

        public ChatHubTests()
        {
            _completionLogicMock = new Mock<ICompletionLogic>();
            _validationLogicMock = new Mock<IValidationHandler>();
            _clientProxyMock = new Mock<IClientProxy>();

            // Mocking IHubClients
            _hubClientsMock = new Mock<IHubClients>();
            _hubClientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object); // Mock the Caller property

            // Mocking HubCallerContext
            _hubCallerContextMock = new Mock<HubCallerContext>();
            _hubCallerContextMock.Setup(c => c).Returns(_hubCallerContextMock.Object); // Mock the Clients property

            // Initialize the ChatHub with the mocked context
            _chatHub = new ChatHub(_completionLogicMock.Object, _validationLogicMock.Object)
            {
                Context = _hubCallerContextMock.Object
            };
        }

        [Fact]
        public async Task Send_ShouldNotSendMessage_WhenRequestIsInvalid()
        {
            // Arrange
            var completionRequest = new CompletionRequest();
            var errorMessage = "Invalid request";

            _validationLogicMock.Setup(v => v.ValidateChatRequest(It.IsAny<CompletionRequest>())).Returns(errorMessage);

            // Act
            await _chatHub.Send(completionRequest);

            // Assert
            _completionLogicMock.Verify(c => c.StreamCompletion(It.IsAny<CompletionRequest>()), Times.Never);
            _clientProxyMock.Verify(c => c.SendAsync("broadcastMessage", It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task Send_ShouldSendMessage_WhenRequestIsValid()
        {
            // Arrange
            var completionRequest = new CompletionRequest();
            var chunks = new[] { "chunk1", "chunk2", "chunk3" };

            _validationLogicMock.Setup(v => v.ValidateChatRequest(It.IsAny<CompletionRequest>())).Returns((string)null);
            _completionLogicMock.Setup(c => c.StreamCompletion(It.IsAny<CompletionRequest>())).Returns(chunks.ToAsyncEnumerable());

            // Act
            await _chatHub.Send(completionRequest);

            // Assert
            _completionLogicMock.Verify(c => c.StreamCompletion(It.IsAny<CompletionRequest>()), Times.Once);
            _clientProxyMock.Verify(c => c.SendAsync("broadcastMessage", It.IsAny<object>()), Times.Exactly(chunks.Length));
        }
    }
}
