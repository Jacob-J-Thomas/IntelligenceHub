namespace IntelligenceHub.Business.Interfaces
{
    using IntelligenceHub.API.DTOs.Auth;
    using IntelligenceHub.DAL.Models;

    /// <summary>
    /// Generates JWT tokens for API authentication.
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Creates a JWT for the specified user.
        /// </summary>
        Auth0Response GenerateToken(DbUser user, bool isAdmin);
    }
}
