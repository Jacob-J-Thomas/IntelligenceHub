using System.Threading.Tasks;
using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.Business;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Controllers;
using IntelligenceHub.Common.Handlers;
using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
using IntelligenceHub.Common;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;

namespace IntelligenceHub.Tests.Unit.Controllers
{
    public class CompletionControllerTests
    {
        private readonly CompletionController _controller;
        private readonly Mock<ICompletionLogic> _mockCompletionLogic;
        private readonly Mock<Settings> _mockSettings;
        private readonly Mock<IValidationHandler> _mockValidationLogic;
        private readonly Mock<HttpContext> _mockHttpContext;

        public CompletionControllerTests()
        {
            _mockCompletionLogic = new Mock<ICompletionLogic>();
            _mockSettings = new Mock<Settings>();
            _mockValidationLogic = new Mock<IValidationHandler>();
            _mockHttpContext = new Mock<HttpContext>();

            // Initialize the controller with mocked dependencies
            _controller = new CompletionController(_mockCompletionLogic.Object, _mockValidationLogic.Object);
        }

        #region Standard Completion
        [Fact]
        public async Task CompletionStandard_Returns200Ok_WhenRequestIsValid()
        {
            // Arrange
            var name = "testProfile";
            var completionRequest = new CompletionRequest { ProfileOptions = new Profile { Name = name } };
            var expectedResponse = new CompletionResponse
            {
                FinishReason = GlobalVariables.FinishReason.Stop,
                Messages = new List<Message>
                {
                    new Message { Role = GlobalVariables.Role.Assistant, Content = "The AI generated string goes here" }
                }
            };

            _mockCompletionLogic.Setup(x => x.ProcessCompletion(completionRequest))
                                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CompletionStandard(name, completionRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task CompletionStandard_Returns400BadRequest_WhenValidationFails()
        {
            // Arrange
            var name = "testProfile";
            var completionRequest = new CompletionRequest { ProfileOptions = new Profile { Name = name } };
            var errorMessage = "Invalid request data";

            _mockValidationLogic.Setup(v => v.ValidateChatRequest(completionRequest))
                                .Returns(errorMessage);

            // Act
            var result = await _controller.CompletionStandard(name, completionRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Equal(errorMessage, badRequestResult.Value);
        }

        [Fact]
        public async Task CompletionStandard_Returns400BadRequest_WhenProcessCompletionReturnsNull()
        {
            // Arrange
            var name = "testProfile";
            var completionRequest = new CompletionRequest { ProfileOptions = new Profile { Name = name } };

            _mockCompletionLogic.Setup(x => x.ProcessCompletion(completionRequest))
                                .ReturnsAsync((CompletionResponse)null);

            // Act
            var result = await _controller.CompletionStandard(name, completionRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Equal("Invalid request. Please check your request body.", badRequestResult.Value);
        }

        [Fact]
        public async Task CompletionStandard_Returns400BadRequest_WhenHttpRequestExceptionThrown()
        {
            // Arrange
            var name = "testProfile";
            var completionRequest = new CompletionRequest { ProfileOptions = new Profile { Name = name } };
            var exceptionMessage = "Request error";

            _mockCompletionLogic.Setup(x => x.ProcessCompletion(completionRequest))
                                .ThrowsAsync(new HttpRequestException(exceptionMessage));

            // Act
            var result = await _controller.CompletionStandard(name, completionRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Equal(exceptionMessage, badRequestResult.Value);
        }

        [Fact]
        public async Task CompletionStandard_Returns500InternalServerError_WhenGenericExceptionThrown()
        {
            // Arrange
            var name = "testProfile";
            var completionRequest = new CompletionRequest { ProfileOptions = new Profile { Name = name } };

            _mockCompletionLogic.Setup(x => x.ProcessCompletion(completionRequest))
                                .ThrowsAsync(new Exception());

            // Act
            var result = await _controller.CompletionStandard(name, completionRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Equal(GlobalVariables.DefaultExceptionMessage, objectResult.Value);
        }
        #endregion

        #region SSE Completion
        [Fact]
        public async Task CompletionStreaming_Returns200Ok_WithSSEHeaders_WhenRequestIsValid()
        {
            // Arrange
            var name = "testProfile";
            var completionRequest = new CompletionRequest { ProfileOptions = new Profile { Name = name } };
            var responseChunks = new List<CompletionStreamChunk>
            {
                new CompletionStreamChunk { FinishReason = GlobalVariables.FinishReason.Stop, CompletionUpdate = "" }
            };

            // Mock the streaming completion response
            _mockCompletionLogic.Setup(x => x.StreamCompletion(completionRequest))
                                .Returns(GetAsyncStream(responseChunks));

            // Mock the headers and set up the response body
            var memoryStream = new MemoryStream();
            var headerDictionary = new HeaderDictionary();
            _mockHttpContext.Setup(ctx => ctx.Response.Headers).Returns(headerDictionary);
            _mockHttpContext.Setup(ctx => ctx.Response.Body).Returns(memoryStream);
            _controller.ControllerContext.HttpContext = _mockHttpContext.Object;

            // Act
            var result = await _controller.CompletionStreaming(name, completionRequest);

            // Assert - Check that headers are set for SSE
            Assert.Equal("text/event-stream", headerDictionary["Content-Type"]);
            Assert.Equal("no-cache", headerDictionary["Cache-Control"]);
            Assert.Equal("keep-alive", headerDictionary["Connection"]);

            // Assert - Check stream content
            var responseBody = Encoding.UTF8.GetString(memoryStream.ToArray());
            foreach (var chunk in responseChunks)
            {
                var jsonChunk = JsonConvert.SerializeObject(chunk);
                var expectedSseMessage = $"data: {jsonChunk}\n\n";
                Assert.Contains(expectedSseMessage, responseBody);
            }

            Assert.IsType<EmptyResult>(result);
        }


        [Fact]
        public async Task CompletionStreaming_Returns400BadRequest_WhenValidationFails()
        {
            // Arrange
            var name = "testProfile";
            var completionRequest = new CompletionRequest { ProfileOptions = new Profile { Name = name } };
            var errorMessage = "Invalid request data";

            _mockValidationLogic.Setup(v => v.ValidateChatRequest(completionRequest))
                                .Returns(errorMessage);

            // Act
            var result = await _controller.CompletionStreaming(name, completionRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Equal(errorMessage, badRequestResult.Value);
        }

        [Fact]
        public async Task CompletionStreaming_Returns400BadRequest_WhenHttpRequestExceptionThrown()
        {
            // Arrange
            var name = "testProfile";
            var completionRequest = new CompletionRequest { ProfileOptions = new Profile { Name = name } };
            var exceptionMessage = GlobalVariables.DefaultExceptionMessage;

            _mockCompletionLogic.Setup(x => x.StreamCompletion(completionRequest)).Throws(new HttpRequestException(exceptionMessage));

            // Act
            var result = await _controller.CompletionStreaming(name, completionRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Equal(exceptionMessage, badRequestResult.Value);
        }

        [Fact]
        public async Task CompletionStreaming_Returns500InternalServerError_WhenGenericExceptionThrown()
        {
            // Arrange
            var name = "testProfile";
            var completionRequest = new CompletionRequest { ProfileOptions = new Profile { Name = name } };

            _mockCompletionLogic.Setup(x => x.StreamCompletion(completionRequest))
                                .Throws(new Exception());

            // Act
            var result = await _controller.CompletionStreaming(name, completionRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Equal(GlobalVariables.DefaultExceptionMessage, objectResult.Value);
        }

        private async IAsyncEnumerable<CompletionStreamChunk> GetAsyncStream(List<CompletionStreamChunk> chunks)
        {
            foreach (var chunk in chunks)
            {
                yield return chunk;
                await Task.Delay(10);
            }
        }
        #endregion
    }
}