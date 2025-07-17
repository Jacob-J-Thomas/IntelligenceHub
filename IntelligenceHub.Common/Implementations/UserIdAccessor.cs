using System.Threading;
using IntelligenceHub.Common.Interfaces;

namespace IntelligenceHub.Common.Implementations
{
    /// <summary>
    /// Default implementation storing the user identifier in an <see cref="AsyncLocal{T}"/> variable.
    /// </summary>
    public class UserIdAccessor : IUserIdAccessor
    {
        private static readonly AsyncLocal<string?> _currentUserId = new();

        /// <inheritdoc />
        public string? UserId
        {
            get => _currentUserId.Value;
            set => _currentUserId.Value = value;
        }
    }
}
