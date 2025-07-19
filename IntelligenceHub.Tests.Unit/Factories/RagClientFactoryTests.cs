using IntelligenceHub.Business.Factories;
using IntelligenceHub.Client.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Runtime.Serialization;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Factories
{
    public class RagClientFactoryTests
    {
        [Fact]
        public void GetClient_ReturnsWeaviateClient()
        {
            var weaviate = (WeaviateSearchServiceClient)FormatterServices.GetUninitializedObject(typeof(WeaviateSearchServiceClient));
            var azure = (AzureAISearchServiceClient)FormatterServices.GetUninitializedObject(typeof(AzureAISearchServiceClient));
            var provider = new Mock<IServiceProvider>();
            provider.Setup(p => p.GetService(typeof(WeaviateSearchServiceClient))).Returns(weaviate);
            provider.Setup(p => p.GetService(typeof(AzureAISearchServiceClient))).Returns(azure);

            var factory = new RagClientFactory(provider.Object);
            var result = factory.GetClient(RagServiceHost.Weaviate);

            Assert.Same(weaviate, result);
        }

        [Fact]
        public void GetClient_ReturnsAzureClient()
        {
            var weaviate = (WeaviateSearchServiceClient)FormatterServices.GetUninitializedObject(typeof(WeaviateSearchServiceClient));
            var azure = (AzureAISearchServiceClient)FormatterServices.GetUninitializedObject(typeof(AzureAISearchServiceClient));
            var provider = new Mock<IServiceProvider>();
            provider.Setup(p => p.GetService(typeof(WeaviateSearchServiceClient))).Returns(weaviate);
            provider.Setup(p => p.GetService(typeof(AzureAISearchServiceClient))).Returns(azure);

            var factory = new RagClientFactory(provider.Object);
            var result = factory.GetClient(RagServiceHost.Azure);

            Assert.Same(azure, result);
        }

        [Fact]
        public void GetClient_InvalidHost_Throws()
        {
            var provider = new Mock<IServiceProvider>();
            var factory = new RagClientFactory(provider.Object);

            Assert.Throws<ArgumentException>(() => factory.GetClient(null));
        }
    }
}
