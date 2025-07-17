using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Repository for managing user subscription items.
    /// </summary>
    public interface IUserSubscriptionItemRepository : IGenericRepository<DbSubscriptionItem>
    {
        /// <summary>
        /// Retrieves a user's subscription item by usage type.
        /// </summary>
        /// <param name="userId">The user's identifier.</param>
        /// <param name="usageType">The usage type.</param>
        /// <returns>The subscription item or null if not found.</returns>
        Task<DbSubscriptionItem?> GetAsync(string userId, string usageType);
    }
}
