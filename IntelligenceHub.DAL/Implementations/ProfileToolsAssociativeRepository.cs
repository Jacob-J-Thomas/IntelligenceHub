using IntelligenceHub.API.DTOs;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL.Tenant;
using IntelligenceHub.Common.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for managing profile-tool associations in the database.
    /// </summary>
    public class ProfileToolsAssociativeRepository : GenericRepository<DbProfileTool>, IProfileToolsAssociativeRepository
    {
        /// <summary>
        /// Constructor for the ProfileToolsAssociativeRepository class.
        /// </summary>
        /// <param name="context">The database context used to map to the SQL database.</param>
        private readonly ITenantProvider _tenantProvider;
        public ProfileToolsAssociativeRepository(IntelligenceHubDbContext context, ITenantProvider tenantProvider) : base(context, tenantProvider)
        {
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Retrieves a list of tool associations for a given profile ID.
        /// </summary>
        /// <param name="profileId">The ID of the profile.</param>
        /// <returns>A list of profile tools.</returns>
        public async Task<List<DbProfileTool>> GetToolAssociationsAsync(int profileId)
        {
            return await _context.ProfileTools
                .Where(pt => pt.ProfileID == profileId && pt.TenantId == _tenantProvider.TenantId)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a list of profile associations for a given tool ID.
        /// </summary>
        /// <param name="toolId">The ID of the tool.</param>
        /// <returns>A list of profile tools.</returns>
        public async Task<List<DbProfileTool>> GetProfileAssociationsAsync(int toolId)
        {
            return await _context.ProfileTools
                .Where(pt => pt.ToolID == toolId && pt.TenantId == _tenantProvider.TenantId)
                .ToListAsync();
        }

        /// <summary>
        /// Associates a list of tools with a profile by their IDs.
        /// </summary>
        /// <param name="profileId">The profile ID.</param>
        /// <param name="toolIds">The list of tool IDs.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> AddAssociationsByProfileIdAsync(int profileId, List<int> toolIds)
        {
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.Id == profileId && p.TenantId == _tenantProvider.TenantId);

            if (profile == null)
            {
                throw new ArgumentException("Profile not found.", nameof(profileId));
            }

            foreach (var toolId in toolIds)
            {
                if (!await _context.ProfileTools.AnyAsync(pt => pt.ProfileID == profileId && pt.ToolID == toolId && pt.TenantId == _tenantProvider.TenantId))
                {
                    _context.ProfileTools.Add(new DbProfileTool
                    {
                        ProfileID = profileId,
                        ToolID = toolId,
                        Profile = profile,
                        TenantId = _tenantProvider.TenantId.Value
                    });
                }
            }
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Associates a list of profiles with a tool by the profile's name.
        /// </summary>
        /// <param name="toolId">The list of tool IDs.</param>
        /// <param name="profileNames">The profile name.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> AddAssociationsByToolIdAsync(int toolId, List<string> profileNames)
        {
            foreach (var name in profileNames)
            {
                var fullName = name.AppendTenant(_tenantProvider.TenantId);
                var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.Name == fullName && p.TenantId == _tenantProvider.TenantId);
                if (profile != null && !await _context.ProfileTools.AnyAsync(pt => pt.ProfileID == profile.Id && pt.ToolID == toolId && pt.TenantId == _tenantProvider.TenantId))
                {
                    _context.ProfileTools.Add(new DbProfileTool { ProfileID = profile.Id, ToolID = toolId, TenantId = _tenantProvider.TenantId.Value });
                }
            }
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Deletes a tool association for a given profile.
        /// </summary>
        /// <param name="toolId">The ID of the tool.</param>
        /// <param name="profileName">The name of the profile.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> DeleteToolAssociationAsync(int toolId, string profileName)
        {
            var fullName = profileName.AppendTenant(_tenantProvider.TenantId);
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.Name == fullName && p.TenantId == _tenantProvider.TenantId);
            if (profile != null)
            {
                var association = await _context.ProfileTools.FirstOrDefaultAsync(pt => pt.ToolID == toolId && pt.ProfileID == profile.Id && pt.TenantId == _tenantProvider.TenantId);
                if (association != null)
                {
                    _context.ProfileTools.Remove(association);
                    return await _context.SaveChangesAsync() > 0;
                }
            }
            return false;
        }

        /// <summary>
        /// Deletes a profile association for a given tool.
        /// </summary>
        /// <param name="profileId">The ID of the profile.</param>
        /// <param name="toolName">The name of the tool.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> DeleteProfileAssociationAsync(int profileId, string toolName)
        {
            var fullName = toolName.AppendTenant(_tenantProvider.TenantId);
            var tool = await _context.Tools.FirstOrDefaultAsync(t => t.Name == fullName && t.TenantId == _tenantProvider.TenantId);
            if (tool != null)
            {
                var association = await _context.ProfileTools.FirstOrDefaultAsync(pt => pt.ProfileID == profileId && pt.ToolID == tool.Id && pt.TenantId == _tenantProvider.TenantId);
                if (association != null)
                {
                    _context.ProfileTools.Remove(association);
                    return await _context.SaveChangesAsync() > 0;
                }
            }
            return false;
        }

        /// <summary>
        /// Deletes all tool associations for a given profile.
        /// </summary>
        /// <param name="profileId">The ID of the profile.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> DeleteAllProfileAssociationsAsync(int profileId)
        {
            var associations = _context.ProfileTools.Where(pt => pt.ProfileID == profileId && pt.TenantId == _tenantProvider.TenantId);
            _context.ProfileTools.RemoveRange(associations);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Deletes all profile associations for a given tool.
        /// </summary>
        /// <param name="toolId">The ID of the tool.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> DeleteAllToolAssociationsAsync(int toolId)
        {
            var associations = _context.ProfileTools.Where(pt => pt.ToolID == toolId && pt.TenantId == _tenantProvider.TenantId);
            _context.ProfileTools.RemoveRange(associations);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}