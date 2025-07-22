using IntelligenceHub.DAL.Models;
using System;

namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Repository for user related operations.
    /// </summary>
    public interface IUserRepository 
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

        /// <summary>
        /// Updates a user entity.
        /// </summary>
        Task<DbUser> UpdateAsync(DbUser user);

        /// <summary>
        /// Atomically increments the monthly request count if the quota is not exceeded.
        /// </summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <param name="now">The current timestamp used to determine the month.</param>
        /// <param name="limit">The allowed monthly request quota.</param>
        /// <returns><c>true</c> if the count was incremented; otherwise, <c>false</c>.</returns>
        Task<bool> TryIncrementMonthlyRequestAsync(int userId, DateTime now, int limit);
    }
}
