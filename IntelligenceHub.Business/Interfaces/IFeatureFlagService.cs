namespace IntelligenceHub.Business.Interfaces
{
    /// <summary>
    /// Provides access to application feature flags.
    /// </summary>
    public interface IFeatureFlagService
    {
        /// <summary>
        /// Gets a value indicating whether Azure AI Search is enabled.
        /// </summary>
        bool UseAzureAISearch { get; }
    }
}
