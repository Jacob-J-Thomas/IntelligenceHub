using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static IntelligenceHub.Common.GlobalVariables;
using IntelligenceHub.DAL.Tenant;

namespace IntelligenceHub.Controllers
{
    /// <summary>
    /// Base controller that resolves the tenant identifier from the auth token.
    /// </summary>
    public abstract class TenantControllerBase : ControllerBase
    {
        private readonly IUserLogic _userLogic;
        protected readonly ITenantProvider _tenantProvider;

        protected TenantControllerBase(IUserLogic userLogic, ITenantProvider tenantProvider)
        {
            _userLogic = userLogic;
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Retrieves the current user and ensures their tenant context is set.
        /// </summary>
        protected async Task<APIResponseWrapper<Guid>> SetUserTenantContextAsync()
        {
            var sub = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(sub))
            {
                return APIResponseWrapper<Guid>.Failure("The user's tenant couldn't be resolved.", APIResponseStatusCodes.InternalError);
            }

            var user = await _userLogic.GetUserBySubAsync(sub);
            if (user == null)
            {
                return APIResponseWrapper<Guid>.Failure("The user's tenant couldn't be resolved.", APIResponseStatusCodes.InternalError);
            }

            _tenantProvider.TenantId = user.TenantId;
            _tenantProvider.User = user;

            return APIResponseWrapper<Guid>.Success(user.TenantId);
        }
    }
}
