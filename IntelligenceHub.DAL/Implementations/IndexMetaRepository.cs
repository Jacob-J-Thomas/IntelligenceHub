using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL.Tenant;
using IntelligenceHub.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for managing index metadata in the database.
    /// </summary>
    public class IndexMetaRepository : GenericRepository<DbIndexMetadata>, IIndexMetaRepository
    {
        /// <summary>
        /// Constructor for the IndexMetaRepository class.
        /// </summary>
        /// <param name="context">The database context used to map to the SQL database.</param>
        private readonly ITenantProvider _tenantProvider;
        public IndexMetaRepository(IntelligenceHubDbContext context, ITenantProvider tenantProvider) : base(context, tenantProvider)
        {
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Retrieves an index metadata entity by its name.
        /// </summary>
        /// <param name="name">The name of the index.</param>
        /// <returns>The index's metadata.</returns>
        public async Task<DbIndexMetadata?> GetByNameAsync(string name)
        {
            var fullName = name.AppendTenant(_tenantProvider.TenantId);
            return await _dbSet.FirstOrDefaultAsync(im => im.Name == fullName && im.TenantId == _tenantProvider.TenantId);
        }
    }
}