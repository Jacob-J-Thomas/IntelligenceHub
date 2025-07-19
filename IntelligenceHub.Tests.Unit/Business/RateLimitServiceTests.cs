using IntelligenceHub.Business.Implementations;
using Microsoft.Extensions.Caching.Memory;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class RateLimitServiceTests
    {
        [Fact]
        public void IsRequestAllowed_EnforcesFreeUserLimit()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new RateLimitService(cache);
            var key = Guid.NewGuid().ToString();

            bool allowed = true;
            for (int i = 0; i < FreeUserRateLimitRequests; i++)
            {
                allowed = service.IsRequestAllowed(key, false);
                Assert.True(allowed);
            }
            allowed = service.IsRequestAllowed(key, false);
            Assert.False(allowed);
        }

        [Fact]
        public void IsRequestAllowed_EnforcesPaidUserLimit()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new RateLimitService(cache);
            var key = Guid.NewGuid().ToString();

            bool allowed = true;
            for (int i = 0; i < PaidUserRateLimitRequests; i++)
            {
                allowed = service.IsRequestAllowed(key, true);
                Assert.True(allowed);
            }
            allowed = service.IsRequestAllowed(key, true);
            Assert.False(allowed);
        }
    }
}
