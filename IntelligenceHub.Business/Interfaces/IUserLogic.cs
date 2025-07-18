using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.Business.Interfaces
{
    /// <summary>
    /// Provides operations for retrieving user information.
    /// </summary>
    public interface IUserLogic
    {
        /// <summary>
        /// Gets a user by Auth0 subject identifier.
        /// </summary>
        Task<DbUser?> GetUserBySubAsync(string sub);
    }
}
