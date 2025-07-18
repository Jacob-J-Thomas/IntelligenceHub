using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for user table operations.
    /// </summary>
    public class UserRepository : GenericRepository<DbUser>, IUserRepository
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UserRepository"/>.
        /// </summary>
        /// <param name="context">Database context.</param>
        public UserRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public async Task<DbUser?> GetBySubAsync(string sub)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Sub == sub);
        }
    }
}
