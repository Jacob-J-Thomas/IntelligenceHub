using IntelligenceHub.Host.Policies;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System.Net.Http;
using Xunit;

namespace IntelligenceHub.Tests.Unit.Host
{
    public class LoadBalancingSelectorTests
    {
        [Fact]
        public void GetNextBaseAddress_CyclesThroughUris()
        {
            var selector = new LoadBalancingSelector(new Dictionary<string,string[]> {
                {"svc", new[]{"http://a","http://b"} }
            });

            Assert.Equal("http://b/", selector.GetNextBaseAddress("svc").ToString());
            Assert.Equal("http://a/", selector.GetNextBaseAddress("svc").ToString());
            Assert.Equal("http://b/", selector.GetNextBaseAddress("svc").ToString());
        }

        [Fact]
        public void RegisterHttpClientWithPolicy_SetsBaseAddress()
        {
            var services = new ServiceCollection();
            var selector = new LoadBalancingSelector(new Dictionary<string,string[]> { {"svc", new[]{"http://a"} } });
            services.AddSingleton(selector);
            LoadBalancingSelector.RegisterHttpClientWithPolicy(services, "pol", "svc", Policy.NoOpAsync<HttpResponseMessage>());
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("pol");
            Assert.Equal("http://a/", client.BaseAddress!.ToString());
        }
    }
}
