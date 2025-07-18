using System;

namespace IntelligenceHub.Business.Interfaces
{
    /// <summary>
    /// Provides rate limiting functionality.
    /// </summary>
    public interface IRateLimitService
    {
        /// <summary>
        /// Determines if a request is allowed for the specified user.
        /// </summary>
        /// <param name="userKey">Unique identifier for the user.</param>
        /// <param name="isPaidUser">Indicates if the user is a paid subscriber.</param>
        /// <returns><c>true</c> if the request is allowed; otherwise, <c>false</c>.</returns>
        bool IsRequestAllowed(string userKey, bool isPaidUser);
    }
}
