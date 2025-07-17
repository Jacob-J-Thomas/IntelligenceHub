namespace IntelligenceHub.Common.Interfaces
{
    /// <summary>
    /// Provides access to the current user identifier for background operations.
    /// </summary>
    public interface IUserIdAccessor
    {
        /// <summary>
        /// Gets or sets the current user identifier.
        /// </summary>
        string? UserId { get; set; }
    }
}
