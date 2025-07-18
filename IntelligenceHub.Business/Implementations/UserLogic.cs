using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Business logic for user related operations.
    /// </summary>
    public class UserLogic : IUserLogic
    {
        private readonly IUserRepository _userRepository;

        public UserLogic(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <inheritdoc/>
        public async Task<DbUser?> GetUserBySubAsync(string sub)
        {
            return await _userRepository.GetBySubAsync(sub);
        }
    }
}
