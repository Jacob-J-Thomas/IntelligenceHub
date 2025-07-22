using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.API.DTOs;
using Microsoft.AspNetCore.Mvc;
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
            if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                return APIResponseWrapper<Guid>.Failure("The user's tenant couldn't be resolved.", APIResponseStatusCodes.InternalError);
            }

            var user = await _userLogic.GetUserByApiTokenAsync(apiKey!);
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
