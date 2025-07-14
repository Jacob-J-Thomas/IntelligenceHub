using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository implementation for user service credentials.
    /// </summary>
    public class UserServiceCredentialRepository : GenericRepository<DbUserServiceCredential>, IUserServiceCredentialRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserServiceCredentialRepository"/> class.
        /// </summary>
        /// <param name="context">The EF database context.</param>
        public UserServiceCredentialRepository(IntelligenceHubDbContext context) : base(context) { }

        /// <inheritdoc />
        public async Task<List<DbUserServiceCredential>> GetByUserIdAsync(string userId)
        {
            return await _dbSet.Where(u => u.UserId == userId).ToListAsync();
        }
    }
}

