using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Business logic for user related operations.
    /// </summary>
    public class UserLogic : IUserLogic
    {
        private readonly IUserRepository _userRepository;
        private readonly string _pepper;

        public UserLogic(IUserRepository userRepository, IConfiguration config)
        {
            _userRepository = userRepository;
            _pepper = config["ApiKeyPepper"] ?? string.Empty;
        }

        /// <inheritdoc/>
        public async Task<DbUser?> GetUserBySubAsync(string sub)
        {
            return await _userRepository.GetBySubAsync(sub);
        }

        /// <inheritdoc/>
        public async Task<DbUser?> GetUserByApiTokenAsync(string apiToken)
        {
            var hash = HashApiKey(apiToken);
            return await _userRepository.GetByApiTokenAsync(hash);
        }

        /// <summary>
        /// Hashes an API key with SHA‑256 + optional pepper.
        /// </summary>
        private string HashApiKey(string apiKey)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(_pepper + apiKey);
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }
    }
}