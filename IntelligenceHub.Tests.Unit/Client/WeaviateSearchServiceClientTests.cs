using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace IntelligenceHub.Tests.Unit.Client
{
    public class WeaviateSearchServiceClientTests
    {
        private WeaviateSearchServiceClient CreateClient()
        {
            var settings = new Mock<IOptionsMonitor<WeaviateSearchServiceClientSettings>>();
            settings.Setup(s => s.CurrentValue).Returns(new WeaviateSearchServiceClientSettings
            {
                Endpoint = "https://example.com",
                Key = "key"
            });

            var httpClient = new HttpClient { BaseAddress = new Uri("https://example.com") };
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            return new WeaviateSearchServiceClient(factory.Object, settings.Object);
        }

        [Fact]
        public void IntToUuidAndBack_RoundTrip()
        {
            var client = CreateClient();
            var toMethod = typeof(WeaviateSearchServiceClient).GetMethod("IntToUuid", BindingFlags.NonPublic | BindingFlags.Static)!;
            var fromMethod = typeof(WeaviateSearchServiceClient).GetMethod("UuidToInt", BindingFlags.NonPublic | BindingFlags.Static)!;
            var uuid = (string)toMethod.Invoke(null, new object[] { 42 })!;
            var id = (int)fromMethod.Invoke(null, new object[] { uuid })!;
            Assert.Equal(42, id);
        }

        [Fact]
        public void ParseIsoUtcDate_BadInput_ReturnsMinValue()
        {
            var method = typeof(WeaviateSearchServiceClient).GetMethod("ParseIsoUtcDate", BindingFlags.NonPublic | BindingFlags.Static)!;
            var token = JToken.Parse("{ }")!;
            var result = (DateTime)method.Invoke(null, new object[] { token, "created" })!;
            Assert.Equal(DateTime.MinValue, result);
        }
    }
}
