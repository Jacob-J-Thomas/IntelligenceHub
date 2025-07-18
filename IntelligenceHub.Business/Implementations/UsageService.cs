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
        private const int FreeTierLimit = 100;
        private readonly IUserRepository _userRepository;

        public UsageService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <inheritdoc/>
        public async Task<APIResponseWrapper<bool>> ValidateAndIncrementUsageAsync(DbUser user)
        {
            if (user.AccessLevel.Equals(AccessLevel.Paid.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return APIResponseWrapper<bool>.Success(true);
            }

            var now = DateTime.UtcNow;
            if (user.RequestMonthStart.Month != now.Month || user.RequestMonthStart.Year != now.Year)
            {
                user.RequestMonthStart = new DateTime(now.Year, now.Month, 1);
                user.RequestsThisMonth = 0;
            }

            if (user.RequestsThisMonth >= FreeTierLimit)
            {
                return APIResponseWrapper<bool>.Failure("The monthly free tier quota has been exceeded.", APIResponseStatusCodes.TooManyRequests);
            }

            user.RequestsThisMonth++;
            await _userRepository.UpdateAsync(user);
            return APIResponseWrapper<bool>.Success(true);
        }
    }
}
