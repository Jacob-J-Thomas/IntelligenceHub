﻿using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Controllers
{
    public class MessageHistoryControllerTests
    {
        private readonly Mock<IMessageHistoryLogic> _mockMessageHistoryLogic;
        private readonly MessageHistoryController _controller;

        public MessageHistoryControllerTests()
        {
            // Mock the MessageHistoryLogic
            _mockMessageHistoryLogic = new Mock<IMessageHistoryLogic>();
            _controller = new MessageHistoryController(_mockMessageHistoryLogic.Object);
        }

        #region GetConversation Tests
        [Fact]
        public async Task GetConversation_ReturnsBadRequest_WhenCountIsLessThanOne()
        {
            // Arrange
            var id = Guid.NewGuid();
            var count = 0;
            var page = 1;

            // Act
            var result = await _controller.GetConversation(id, page, count);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Count must be greater than 0.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetConversation_ReturnsNotFound_WhenConversationDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            var count = 1;
            var page = 1;
            _mockMessageHistoryLogic.Setup(x => x.GetConversationHistory(id, page, count)).ReturnsAsync(APIResponseWrapper<List<Message>>.Failure($"A conversation with the id '{id}' was not found.", APIResponseStatusCodes.NotFound));

            // Act
            var result = await _controller.GetConversation(id, count, page);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"A conversation with the id '{id}' was not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetConversation_ReturnsOk_WhenConversationExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var count = 1;
            var page = 1;
            var messages = new List<Message> { new Message { Role = IntelligenceHub.Common.GlobalVariables.Role.User, Content = "Test" } };
            var response = APIResponseWrapper<List<Message>>.Success(messages);
            _mockMessageHistoryLogic.Setup(x => x.GetConversationHistory(id, page, count)).ReturnsAsync(response);

            // Act
            var result = await _controller.GetConversation(id, page, count);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(messages, okResult.Value);
        }
        #endregion

        #region UpsertConversationData Tests
        [Fact]
        public async Task UpsertConversationData_ReturnsBadRequest_WhenMessagesAreNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            List<Message> messages = null;

            // Act
            var result = await _controller.UpsertConversationData(id, messages);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Messages must be included in the request.", badRequestResult.Value); // assuming this is the case in your controller.
        }

        [Fact]
        public async Task UpsertConversationData_ReturnsOk_WhenMessagesAreSuccessfullyAdded()
        {
            // Arrange
            var id = Guid.NewGuid();
            var messages = new List<Message> { new Message { Role = IntelligenceHub.Common.GlobalVariables.Role.User, Content = "Test" } };
            var mockResult = APIResponseWrapper<List<Message>>.Success(messages);
            _mockMessageHistoryLogic.Setup(x => x.UpdateOrCreateConversation(id, messages)).ReturnsAsync(mockResult);

            // Act
            var result = await _controller.UpsertConversationData(id, messages);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(messages, okResult.Value);
        }
        #endregion

        #region DeleteConversation Tests
        [Fact]
        public async Task DeleteConversation_ReturnsNotFound_WhenConversationDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockMessageHistoryLogic.Setup(x => x.DeleteConversation(id)).ReturnsAsync(APIResponseWrapper<bool>.Failure($"No conversation with ID '{id}' was found", APIResponseStatusCodes.NotFound));

            // Act
            var result = await _controller.DeleteConversation(id);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"No conversation with ID '{id}' was found", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteConversation_ReturnsOk_WhenConversationIsDeleted()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockMessageHistoryLogic.Setup(x => x.DeleteConversation(id)).ReturnsAsync(APIResponseWrapper<bool>.Success(true));

            // Act
            var result = await _controller.DeleteConversation(id);

            // Assert
            var okResult = Assert.IsType<NoContentResult>(result);
        }
        #endregion

        #region DeleteMessage Tests
        [Fact]
        public async Task DeleteMessage_ReturnsNotFound_WhenMessageDoesNotExist()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var messageId = 1;
            _mockMessageHistoryLogic.Setup(x => x.DeleteMessage(conversationId, messageId)).ReturnsAsync(APIResponseWrapper<bool>.Failure("The conversation or message was not found", APIResponseStatusCodes.NotFound));

            // Act
            var result = await _controller.DeleteMessage(conversationId, messageId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("The conversation or message was not found", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteMessage_ReturnsOk_WhenMessageIsDeleted()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var messageId = 1;
            _mockMessageHistoryLogic.Setup(x => x.DeleteMessage(conversationId, messageId)).ReturnsAsync(APIResponseWrapper<bool>.Success(true));

            // Act
            var result = await _controller.DeleteMessage(conversationId, messageId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }
        #endregion
    }
}