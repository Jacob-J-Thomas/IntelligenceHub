namespace IntelligenceHub.Client.Interfaces
{
    /// <summary>
    /// Provides user-specific service credentials.
    /// </summary>
    public interface IUserCredentialProvider
    {
        /// <summary>
        /// Retrieves a credential for the current user.
        /// </summary>
        /// <param name="serviceType">The service type, e.g. AGI or RAG.</param>
        /// <param name="host">The optional host/provider name.</param>
        /// <returns>The credential or null if none were found.</returns>
        Task<IntelligenceHub.DAL.Models.DbUserServiceCredential?> GetCredentialAsync(string serviceType, string? host);
    }
}
