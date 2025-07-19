using IntelligenceHub.Business.Factories;
using IntelligenceHub.Client.Implementations;
using Moq;
using static IntelligenceHub.Common.GlobalVariables;
using Microsoft.Extensions.Options;
using System.Net.Http;
using IntelligenceHub.Common.Config;
using System;
using System.Collections.Generic;

namespace IntelligenceHub.Tests.Unit.Factories
{
    public class AGIClientFactoryTests
    {
        private IServiceProvider CreateProvider()
        {
            var options = new Mock<IOptionsMonitor<AGIClientSettings>>();
            options.Setup(o => o.CurrentValue).Returns(new AGIClientSettings
            {
                AzureOpenAIServices = new List<AGIServiceDetails>
                {
                    new AGIServiceDetails { Endpoint = "https://azure/", Key = "a" },
                    new AGIServiceDetails { Endpoint = "https://openai/", Key = "o" }
                },
                AnthropicServices = new List<AGIServiceDetails>
                {
                    new AGIServiceDetails { Endpoint = "https://anthropic/", Key = "x" }
                }
            });

            var httpFactory = new Mock<IHttpClientFactory>();
            httpFactory.Setup(f => f.CreateClient(ClientPolicies.AzureAIClientPolicy.ToString()))
                .Returns(new HttpClient { BaseAddress = new Uri("https://azure/") });
            httpFactory.Setup(f => f.CreateClient(ClientPolicies.OpenAIClientPolicy.ToString()))
                .Returns(new HttpClient { BaseAddress = new Uri("https://openai/") });
            httpFactory.Setup(f => f.CreateClient(ClientPolicies.AnthropicAIClientPolicy.ToString()))
                .Returns(new HttpClient { BaseAddress = new Uri("https://anthropic/") });

            var provider = new Mock<IServiceProvider>();
            provider.Setup(p => p.GetService(typeof(IOptionsMonitor<AGIClientSettings>))).Returns(options.Object);
            provider.Setup(p => p.GetService(typeof(IHttpClientFactory))).Returns(httpFactory.Object);
            return provider.Object;
        }

        [Fact]
        public void GetClient_ReturnsAzureClient_ForOpenAI()
        {
            var provider = CreateProvider();
            var factory = new AGIClientFactory(provider);
            var result = factory.GetClient(AGIServiceHost.OpenAI);

            Assert.IsType<AzureAIClient>(result);
        }

        [Fact]
        public void GetClient_ReturnsAzureClient()
        {
            var provider = CreateProvider();
            var factory = new AGIClientFactory(provider);
            var result = factory.GetClient(AGIServiceHost.Azure);

            Assert.IsType<AzureAIClient>(result);
        }

        [Fact]
        public void GetClient_ReturnsAzureClient_ForAnthropic()
        {
            var provider = CreateProvider();
            var factory = new AGIClientFactory(provider);
            var result = factory.GetClient(AGIServiceHost.Anthropic);

            Assert.IsType<AzureAIClient>(result);
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
