using IntelligenceHub.API.DTOs.Auth;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.DAL.Models;
using System.Threading.Tasks;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Provides authentication logic for the application.
    /// </summary>
    public class AuthLogic : IAuthLogic
    {
        private readonly IJwtService _jwtService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthLogic"/> class.
        /// </summary>
        /// <param name="jwtService">Service used to create JWTs.</param>
        public AuthLogic(IJwtService jwtService)
        {
            _jwtService = jwtService;
        }

        /// <summary>
        /// Gets the default authentication token.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Auth0 response.</returns>
        public Task<Auth0Response?> GetDefaultAuthToken(DbUser user)
        {
            return Task.FromResult<Auth0Response?>(_jwtService.GenerateToken(user, false));
        }

        /// <summary>
        /// Gets the admin authentication token.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Auth0 response.</returns>
        public Task<Auth0Response?> GetAdminAuthToken(DbUser user)
        {
            return Task.FromResult<Auth0Response?>(_jwtService.GenerateToken(user, true));
        }
    }
}
