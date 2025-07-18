using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common.Tenant;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IntelligenceHub.Controllers
{
    /// <summary>
    /// Base controller that resolves the tenant identifier from the auth token.
    /// </summary>
    public abstract class TenantControllerBase : ControllerBase
    {
        private readonly IUserLogic _userLogic;
        private readonly ITenantProvider _tenantProvider;

        protected TenantControllerBase(IUserLogic userLogic, ITenantProvider tenantProvider)
        {
            _userLogic = userLogic;
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Retrieves the tenant identifier for the current user.
        /// </summary>
        protected async Task<Guid?> GetTenantIdAsync()
        {
            var sub = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(sub))
                return null;

            var user = await _userLogic.GetUserBySubAsync(sub);
            if (user?.TenantId != null)
            {
                _tenantProvider.TenantId = user.TenantId;
            }
            return user?.TenantId;
        }
    }
}
