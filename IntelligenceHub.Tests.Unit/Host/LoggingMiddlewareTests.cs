using IntelligenceHub.Host.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace IntelligenceHub.Tests.Unit.Host
{
    public class LoggingMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_LogsRequestAndResponse()
        {
            var logger = new Mock<ILogger<LoggingMiddleware>>();
            var middleware = new LoggingMiddleware(context => {
                context.Response.StatusCode = 200;
                return Task.CompletedTask;
            }, logger.Object);
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/test";
            await middleware.InvokeAsync(context);
            logger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeast(2));
        }
    }
}
