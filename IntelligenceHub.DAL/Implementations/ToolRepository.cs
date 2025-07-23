using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL.Tenant;
using IntelligenceHub.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for managing tools in the database.
    /// </summary>
    public class ToolRepository : GenericRepository<DbTool>, IToolRepository
    {
        /// <summary>
        /// Constructor for the ToolRepository class.
        /// </summary>
        /// <param name="context">The database context used to map to the SQL database.</param>
        private readonly ITenantProvider _tenantProvider;
        public ToolRepository(IntelligenceHubDbContext context, ITenantProvider tenantProvider) : base(context, tenantProvider)
        {
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Retrieves a tool from the database by name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>The tool, or null if none is found.</returns>
        public async Task<DbTool?> GetByNameAsync(string name)
        {
            var fullName = name.AppendTenant(_tenantProvider.TenantId);
            return await _dbSet.FirstOrDefaultAsync(t => t.Name == fullName && t.TenantId == _tenantProvider.TenantId);
        }

        /// <summary>
        /// Retrieves a tool from the database by ID.
        /// </summary>
        /// <param name="id">The ID of the tool.</param>
        /// <returns>The tool, or null if none is found.</returns>
        public async Task<DbTool?> GetByIdAsync(int id)
        {
            return await _context.Tools.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == _tenantProvider.TenantId);
        }

        /// <summary>
        /// Retrieves a list of tools associated with a given profile name.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>A list of tool names.</returns>
        public async Task<List<string>> GetProfileToolsAsync(string name)
        {
            var fullName = name.AppendTenant(_tenantProvider.TenantId);
            var profileTools = await _context.ProfileTools
                .Include(pt => pt.Tool)
                .Include(pt => pt.Profile)
                .Where(pt => pt.Profile.Name == fullName && pt.TenantId == _tenantProvider.TenantId)
                .Select(pt => pt.Tool.Name)
                .ToListAsync();

            return profileTools.Select(t => t.RemoveTenant()).ToList();
        }

        /// <summary>
        /// Retrieves a list of profiles associated with a given tool name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>A list of profile names.</returns>
        public async Task<List<string>> GetToolProfilesAsync(string name)
        {
            var fullName = name.AppendTenant(_tenantProvider.TenantId);
            var toolProfiles = await _context.ProfileTools
                .Include(pt => pt.Tool)
                .Include(pt => pt.Profile)
                .Where(pt => pt.Tool.Name == fullName && pt.TenantId == _tenantProvider.TenantId)
                .Select(pt => pt.Profile.Name)
                .ToListAsync();

            return toolProfiles.Select(p => p.RemoveTenant()).ToList();
        }
    }
}