using IntelligenceHub.Business.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Default implementation of <see cref="IRateLimitService"/>.
    /// </summary>
    public class RateLimitService : IRateLimitService
    {
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitService"/> class.
        /// </summary>
        /// <param name="cache">The memory cache used for storing request counts.</param>
        public RateLimitService(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <inheritdoc/>
        public bool IsRequestAllowed(string userKey, bool isPaidUser)
        {
            var limit = isPaidUser ? PaidUserRateLimitRequests : FreeUserRateLimitRequests;
            var window = isPaidUser ? PaidUserRateLimitWindowSeconds : FreeUserRateLimitWindowSeconds;
            var key = $"rl_{userKey}";

            var entry = _cache.GetOrCreate(key, e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(window);
                return new RateLimitEntry();
            });

            if (entry.Count >= limit)
            {
                return false;
            }

            entry.Count++;
            _cache.Set(key, entry, TimeSpan.FromSeconds(window));
            return true;
        }

        private class RateLimitEntry
        {
            public int Count { get; set; }
        }
    }
}
