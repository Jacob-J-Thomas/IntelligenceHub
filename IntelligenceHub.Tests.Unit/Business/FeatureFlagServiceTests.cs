using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;
using Moq;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class FeatureFlagServiceTests
    {
        [Fact]
        public void UseAzureAISearch_ReturnsCurrentValue()
        {
            var options = new Mock<IOptionsMonitor<FeatureFlagSettings>>();
            options.Setup(o => o.CurrentValue).Returns(new FeatureFlagSettings { UseAzureAISearch = true });
            var service = new FeatureFlagService(options.Object);

            Assert.True(service.UseAzureAISearch);
        }

        [Fact]
        public void UseAzureAISearch_ReturnsFalse_WhenDisabled()
        {
            var options = new Mock<IOptionsMonitor<FeatureFlagSettings>>();
            options.Setup(o => o.CurrentValue).Returns(new FeatureFlagSettings { UseAzureAISearch = false });
            var service = new FeatureFlagService(options.Object);

            Assert.False(service.UseAzureAISearch);
        }
    }
}
