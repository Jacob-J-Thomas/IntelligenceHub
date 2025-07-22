using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
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
            var tenantClaim = User.Claims.FirstOrDefault(c => c.Type == TenantIdClaim)?.Value;
            if (tenantClaim is null || !Guid.TryParse(tenantClaim, out var tenantId))
            {
                return APIResponseWrapper<Guid>.Failure("The user's tenant couldn't be resolved.", APIResponseStatusCodes.InternalError);
            }

            var user = await _userLogic.GetUserByTenantIdAsync(tenantId);
            if (user == null)
            {
                return APIResponseWrapper<Guid>.Failure("The user's tenant couldn't be resolved.", APIResponseStatusCodes.InternalError);
            }

            _tenantProvider.TenantId = tenantId;
            _tenantProvider.User = user;

            return APIResponseWrapper<Guid>.Success(tenantId);
        }
    }
}
