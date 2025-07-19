using IntelligenceHub.Business.Factories;
using IntelligenceHub.Client.Implementations;
using Moq;
using System.Runtime.Serialization;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Factories
{
    public class AGIClientFactoryTests
    {
        [Fact]
        public void GetClient_ReturnsOpenAIClient()
        {
            var open = (OpenAIClient)FormatterServices.GetUninitializedObject(typeof(OpenAIClient));
            var azure = (AzureAIClient)FormatterServices.GetUninitializedObject(typeof(AzureAIClient));
            var anthropic = (AnthropicAIClient)FormatterServices.GetUninitializedObject(typeof(AnthropicAIClient));
            var provider = new Mock<IServiceProvider>();
            provider.Setup(p => p.GetService(typeof(OpenAIClient))).Returns(open);
            provider.Setup(p => p.GetService(typeof(AzureAIClient))).Returns(azure);
            provider.Setup(p => p.GetService(typeof(AnthropicAIClient))).Returns(anthropic);

            var factory = new AGIClientFactory(provider.Object);
            var result = factory.GetClient(AGIServiceHost.OpenAI);

            Assert.Same(open, result);
        }

        [Fact]
        public void GetClient_ReturnsAzureClient()
        {
            var open = (OpenAIClient)FormatterServices.GetUninitializedObject(typeof(OpenAIClient));
            var azure = (AzureAIClient)FormatterServices.GetUninitializedObject(typeof(AzureAIClient));
            var anthropic = (AnthropicAIClient)FormatterServices.GetUninitializedObject(typeof(AnthropicAIClient));
            var provider = new Mock<IServiceProvider>();
            provider.Setup(p => p.GetService(typeof(OpenAIClient))).Returns(open);
            provider.Setup(p => p.GetService(typeof(AzureAIClient))).Returns(azure);
            provider.Setup(p => p.GetService(typeof(AnthropicAIClient))).Returns(anthropic);

            var factory = new AGIClientFactory(provider.Object);
            var result = factory.GetClient(AGIServiceHost.Azure);

            Assert.Same(azure, result);
        }

        [Fact]
        public void GetClient_ReturnsAnthropicClient()
        {
            var open = (OpenAIClient)FormatterServices.GetUninitializedObject(typeof(OpenAIClient));
            var azure = (AzureAIClient)FormatterServices.GetUninitializedObject(typeof(AzureAIClient));
            var anthropic = (AnthropicAIClient)FormatterServices.GetUninitializedObject(typeof(AnthropicAIClient));
            var provider = new Mock<IServiceProvider>();
            provider.Setup(p => p.GetService(typeof(OpenAIClient))).Returns(open);
            provider.Setup(p => p.GetService(typeof(AzureAIClient))).Returns(azure);
            provider.Setup(p => p.GetService(typeof(AnthropicAIClient))).Returns(anthropic);

            var factory = new AGIClientFactory(provider.Object);
            var result = factory.GetClient(AGIServiceHost.Anthropic);

            Assert.Same(anthropic, result);
        }

        [Fact]
        public void GetClient_InvalidHost_Throws()
        {
            var provider = new Mock<IServiceProvider>();
            var factory = new AGIClientFactory(provider.Object);

            Assert.Throws<ArgumentException>(() => factory.GetClient(null));
        }
    }
}
