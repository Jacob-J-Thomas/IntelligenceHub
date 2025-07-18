using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for managing profiles in the database.
    /// </summary>
    public class ProfileRepository : GenericRepository<DbProfile>, IProfileRepository
    {
        /// <summary>
        /// Constructor for the ProfileRepository class.
        /// </summary>
        /// <param name="context">The database context used to map to the SQL database.</param>
        private readonly ITenantProvider _tenantProvider;
        public ProfileRepository(IntelligenceHubDbContext context, ITenantProvider tenantProvider) : base(context, tenantProvider)
        {
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Retrieves a profiles from the database by name.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>The matching profile, or null if no results are found.</returns>
        public async Task<DbProfile?> GetByNameAsync(string name)
        {
            return await _dbSet
                .Include(p => p.ProfileTools)
                .ThenInclude(pt => pt.Tool)
                .ThenInclude(t => t.Properties)
                .FirstOrDefaultAsync(p => p.Name == name && p.TenantId == _tenantProvider.TenantId);
        }

        /// <summary>
        /// Retrieves a profiles from the database by name.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>The matching profile, or null if no results are found.</returns>
        public async Task<DbProfile?> GetAsync(int id)
        {
            return await _dbSet
                .Include(p => p.ProfileTools)
                .ThenInclude(pt => pt.Tool)
                .ThenInclude(t => t.Properties)
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _tenantProvider.TenantId);
        }
    }
}