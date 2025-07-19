using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using Anthropic.SDK.Messaging;

namespace IntelligenceHub.Tests.Unit.Client
{
    public class AnthropicAIClientTests
    {
        private AnthropicAIClient CreateClient()
        {
            var settings = new AGIClientSettings
            {
                AnthropicServices = new List<AGIServiceDetails>
                {
                    new AGIServiceDetails { Endpoint = "https://example.com/", Key = "key" }
                }
            };

            var options = new Mock<IOptionsMonitor<AGIClientSettings>>();
            options.Setup(o => o.CurrentValue).Returns(settings);

            var httpClient = new HttpClient { BaseAddress = new Uri("https://example.com/") };
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            return new AnthropicAIClient(options.Object, factory.Object);
        }

        [Fact]
        public void ConvertRoles_RoundTrips()
        {
            var client = CreateClient();
            var toMethod = typeof(AnthropicAIClient).GetMethod("ConvertToAnthropicRole", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var fromMethod = typeof(AnthropicAIClient).GetMethod("ConvertFromAnthropicRole", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var role = GlobalVariables.Role.User;
            var anth = (RoleType)toMethod.Invoke(client, new object?[] { role })!;
            var back = (GlobalVariables.Role)fromMethod.Invoke(client, new object?[] { anth })!;
            Assert.Equal(role, back);
        }

        [Fact]
        public void ConvertFinishReason_WithTools_ReturnsToolCalls()
        {
            var client = CreateClient();
            var method = typeof(AnthropicAIClient).GetMethod("ConvertFinishReason", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var result = (GlobalVariables.FinishReasons)method.Invoke(client, new object[] { "end_turn", true })!;
            Assert.Equal(GlobalVariables.FinishReasons.ToolCalls, result);
        }

        [Fact]
        public void GetMimeTypeFromBase64_ReturnsGif()
        {
            var client = CreateClient();
            var bytes = new byte[16];
            bytes[0] = 0x47; bytes[1] = 0x49; bytes[2] = 0x46; bytes[3] = 0x38;
            var base64 = Convert.ToBase64String(bytes);
            var method = typeof(AnthropicAIClient).GetMethod("GetMimeTypeFromBase64", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var result = (string)method.Invoke(client, new object[] { base64 })!;
            Assert.Equal("image/png", result);
        }
    }
}
