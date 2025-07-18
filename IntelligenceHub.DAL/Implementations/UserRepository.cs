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
        private readonly IntelligenceHubDbContext _context;
        protected readonly DbSet<DbUser> _dbSet;

        /// <summary>
        /// Initializes a new instance of <see cref="UserRepository"/>.
        /// </summary>
        /// <param name="context">Database context.</param>
        public UserRepository(IntelligenceHubDbContext context)
        {
            _context = context;
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

        /// <inheritdoc/>
        public async Task<DbUser> UpdateAsync(DbUser user)
        {
            _dbSet.Attach(user);
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            await _context.Entry(user).ReloadAsync();
            return user;
        }
    }
}
