using IntelligenceHub.Business.Interfaces;
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

        protected TenantControllerBase(IUserLogic userLogic)
        {
            _userLogic = userLogic;
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
            return user?.TenantId;
        }
    }
}
