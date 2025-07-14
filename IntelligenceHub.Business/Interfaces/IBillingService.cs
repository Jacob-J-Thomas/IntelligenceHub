using System.Threading.Tasks;

namespace IntelligenceHub.Business.Interfaces
{
    /// <summary>
    /// Handles billing interactions with Stripe.
    /// </summary>
    public interface IBillingService
    {
        /// <summary>
        /// Records usage for the provided subscription item.
        /// </summary>
        /// <param name="subscriptionItemId">The Stripe subscription item identifier.</param>
        /// <param name="quantity">Amount of usage to record.</param>
        Task TrackUsageAsync(string subscriptionItemId, long quantity);
    }
}
