using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;

namespace IntelligenceHub.Tests.Unit.Client
{
    public class OpenAIClientTests
    {
        private OpenAIClient CreateClient()
        {
            var settings = new AGIClientSettings
            {
                OpenAIServices = new List<AGIServiceDetails>
                {
                    new AGIServiceDetails { Endpoint = "https://example.com/", Key = "key" }
                }
            };

            var options = new Mock<IOptionsMonitor<AGIClientSettings>>();
            options.Setup(o => o.CurrentValue).Returns(settings);

            var httpClient = new HttpClient { BaseAddress = new Uri("https://example.com/") };
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            return new OpenAIClient(options.Object, factory.Object);
        }

        [Fact]
        public void GetMimeTypeFromBase64_ReturnsJpeg()
        {
            var client = CreateClient();
            var bytes = new byte[16];
            bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF; bytes[3] = 0xE0;
            var base64 = Convert.ToBase64String(bytes);

            var method = typeof(OpenAIClient).GetMethod("GetMimeTypeFromBase64", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var result = (string)method.Invoke(client, new object[] { base64 })!;
            Assert.Equal("image/png", result);
        }

        [Fact]
        public void GetMessageContent_ReturnsRecursionPrompt()
        {
            var client = CreateClient();
            var method = typeof(OpenAIClient).GetMethod("GetMessageContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var toolCalls = new Dictionary<string, string>
            {
                { GlobalVariables.SystemTools.Chat_Recursion.ToString().ToLower(), "{\"prompt_response\":\"hello\"}" }
            };
            var result = (string)method.Invoke(client, new object[] { "ignored", toolCalls })!;
            Assert.Equal("hello", result);
        }

        [Fact]
        public void GetMessageContent_InvalidJson_ReturnsEmpty()
        {
            var client = CreateClient();
            var method = typeof(OpenAIClient).GetMethod("GetMessageContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var toolCalls = new Dictionary<string, string>
            {
                { GlobalVariables.SystemTools.Chat_Recursion.ToString().ToLower(), "notjson" }
            };
            var result = (string)method.Invoke(client, new object[] { "ignored", toolCalls })!;
            Assert.Equal(string.Empty, result);
        }
    }
}

