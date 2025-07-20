using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common;
using static IntelligenceHub.Common.GlobalVariables;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;

namespace IntelligenceHub.Tests.Unit.Client
{
    public class AzureAIClientTests
    {
        private AzureAIClient CreateClient()
        {
            var settings = new AGIClientSettings
            {
                AzureOpenAIServices = new List<AGIServiceDetails>
                {
                    new AGIServiceDetails { Endpoint = "https://example.com/", Key = "key" }
                }
            };

            var options = new Mock<IOptionsMonitor<AGIClientSettings>>();
            options.Setup(o => o.CurrentValue).Returns(settings);

            var httpClient = new HttpClient { BaseAddress = new Uri("https://example.com/") };
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            return new AzureAIClient(options.Object, factory.Object, AGIServiceHost.Azure);
        }

        [Fact]
        public void GetMimeTypeFromBase64_ReturnsPng()
        {
            var client = CreateClient();
            var bytes = new byte[16];
            bytes[0] = 0x89; bytes[1] = 0x50; bytes[2] = 0x4E; bytes[3] = 0x47;
            var base64 = Convert.ToBase64String(bytes);

            var method = typeof(AzureAIClient).GetMethod("GetMimeTypeFromBase64", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var result = (string)method.Invoke(client, new object[] { base64 })!;
            Assert.Equal("image/png", result);
        }

        [Fact]
        public void GetMessageContent_InvalidJson_ReturnsEmpty()
        {
            var client = CreateClient();
            var method = typeof(AzureAIClient).GetMethod("GetMessageContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var toolCalls = new Dictionary<string, string>
            {
                { GlobalVariables.SystemTools.Chat_Recursion.ToString().ToLower(), "bad" }
            };
            var result = (string)method.Invoke(client, new object[] { "ignored", toolCalls })!;
            Assert.Equal(string.Empty, result);
        }
    }
}
