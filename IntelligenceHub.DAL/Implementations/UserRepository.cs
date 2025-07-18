using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for user table operations. NOTE: Unlike all other constructors, this repository does not extend IGenericRepository.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        protected readonly DbSet<DbUser> _dbSet;

        /// <summary>
        /// Initializes a new instance of <see cref="UserRepository"/>.
        /// </summary>
        /// <param name="context">Database context.</param>
        public UserRepository(IntelligenceHubDbContext context)
        {
            _dbSet = context.Set<DbUser>();
        }

        /// <inheritdoc/>
        public async Task<DbUser?> GetBySubAsync(string sub)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Sub == sub);
        }

        /// <inheritdoc/>
        public async Task<DbUser?> GetByApiTokenAsync(string apiToken)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.ApiToken == apiToken);
        }
    }
}
