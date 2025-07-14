using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace IntelligenceHub.Client.Implementations
{
    /// <summary>
    /// Retrieves user service credentials from the database.
    /// </summary>
    public class UserCredentialProvider : IUserCredentialProvider
    {
        private readonly IUserServiceCredentialRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserCredentialProvider(IUserServiceCredentialRepository repository, IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public async Task<DbUserServiceCredential?> GetCredentialAsync(string serviceType, string? host)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;

            var creds = await _repository.GetByUserIdAsync(userId);
            return creds.FirstOrDefault(c =>
                c.ServiceType.Equals(serviceType, StringComparison.OrdinalIgnoreCase) &&
                (host == null || (c.Host != null && c.Host.Equals(host, StringComparison.OrdinalIgnoreCase))));
        }
    }
}
