using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository implementation for user subscription items.
    /// </summary>
    public class UserSubscriptionItemRepository : GenericRepository<DbSubscriptionItem>, IUserSubscriptionItemRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserSubscriptionItemRepository"/> class.
        /// </summary>
        /// <param name="context">The EF database context.</param>
        public UserSubscriptionItemRepository(IntelligenceHubDbContext context) : base(context) { }

        /// <inheritdoc />
        public async Task<DbSubscriptionItem?> GetAsync(string userId, string usageType)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.UserId == userId && s.UsageType == usageType);
        }
    }
}
