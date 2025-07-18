using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Models;
using System.Threading.Tasks;

namespace IntelligenceHub.Business.Interfaces
{
    /// <summary>
    /// Provides operations for tracking user usage.
    /// </summary>
    public interface IUsageService
    {
        /// <summary>
        /// Validates and increments a user's request count.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <returns>An APIResponseWrapper indicating success or failure.</returns>
        Task<APIResponseWrapper<bool>> ValidateAndIncrementUsageAsync(DbUser user);
    }
}
