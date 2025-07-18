using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Repository for user related operations.
    /// </summary>
    public interface IUserRepository : IGenericRepository<DbUser>
    {
        /// <summary>
        /// Retrieves a user entity by the Auth0 subject identifier.
        /// </summary>
        /// <param name="sub">The subject identifier from the auth token.</param>
        /// <returns>The user if found; otherwise null.</returns>
        Task<DbUser?> GetBySubAsync(string sub);

        /// <summary>
        /// Retrieves a user entity by API token.
        /// </summary>
        /// <param name="apiToken">API token issued to the user.</param>
        /// <returns>The user if found; otherwise null.</returns>
        Task<DbUser?> GetByApiTokenAsync(string apiToken);
    }
}
