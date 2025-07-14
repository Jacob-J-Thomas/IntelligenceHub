using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Repository for managing user provided service credentials.
    /// </summary>
    public interface IUserServiceCredentialRepository : IGenericRepository<DbUserServiceCredential>
    {
        /// <summary>
        /// Retrieves all credentials associated with a user.
        /// </summary>
        /// <param name="userId">The user's identifier.</param>
        /// <returns>A list of credentials.</returns>
        Task<List<DbUserServiceCredential>> GetByUserIdAsync(string userId);
    }
}
