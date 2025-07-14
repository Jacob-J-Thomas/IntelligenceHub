using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;
using Stripe;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Stripe based billing service for metered usage tracking.
    /// </summary>
    public class StripeBillingService : IBillingService
    {
        private readonly StripeSettings _settings;
        private readonly SubscriptionItemUsageRecordService _usageRecordService;

        /// <summary>
        /// Initializes a new instance of the <see cref="StripeBillingService"/> class.
        /// </summary>
        public StripeBillingService(IOptions<StripeSettings> options)
        {
            _settings = options.Value;
            StripeConfiguration.ApiKey = _settings.ApiKey;
            _usageRecordService = new SubscriptionItemUsageRecordService();
        }

        /// <inheritdoc />
        public async Task TrackUsageAsync(string subscriptionItemId, long quantity)
        {
            var usageRecordOptions = new SubscriptionItemUsageRecordCreateOptions
            {
                Action = "increment",
                Quantity = quantity,
                Timestamp = DateTime.UtcNow
            };
            await _usageRecordService.CreateAsync(subscriptionItemId, usageRecordOptions);
        }
    }
}
