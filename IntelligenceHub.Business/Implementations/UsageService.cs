using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using System;
using System.Threading.Tasks;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Default implementation of <see cref="IUsageService"/>.
    /// </summary>
    public class UsageService : IUsageService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRateLimitService _rateLimitService;

        public UsageService(IUserRepository userRepository, IRateLimitService rateLimitService)
        {
            _userRepository = userRepository;
            _rateLimitService = rateLimitService;
        }

        /// <inheritdoc/>
        public async Task<APIResponseWrapper<bool>> ValidateAndIncrementUsageAsync(DbUser user)
        {
            var isPaid = user.AccessLevel.Equals(AccessLevel.Paid.ToString(), StringComparison.OrdinalIgnoreCase);

            if (!_rateLimitService.IsRequestAllowed(user.Id.ToString(), isPaid))
            {
                return APIResponseWrapper<bool>.Failure("Rate limit exceeded.", APIResponseStatusCodes.TooManyRequests);
            }

            if (isPaid)
            {
                return APIResponseWrapper<bool>.Success(true);
            }

            var now = DateTime.UtcNow;
            if (user.RequestMonthStart.Month != now.Month || user.RequestMonthStart.Year != now.Year)
            {
                user.RequestMonthStart = new DateTime(now.Year, now.Month, 1);
                user.RequestsThisMonth = 0;
            }

            if (user.RequestsThisMonth >= FreeTierMonthlyLimit)
            {
                return APIResponseWrapper<bool>.Failure("The monthly free tier quota has been exceeded.", APIResponseStatusCodes.TooManyRequests);
            }

            user.RequestsThisMonth++;
            await _userRepository.UpdateAsync(user);
            return APIResponseWrapper<bool>.Success(true);
        }
    }
}
