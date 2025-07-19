using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Default implementation of <see cref="IFeatureFlagService"/>.
    /// </summary>
    public class FeatureFlagService : IFeatureFlagService
    {
        private readonly IOptionsMonitor<FeatureFlagSettings> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureFlagService"/> class.
        /// </summary>
        /// <param name="options">The feature flag options.</param>
        public FeatureFlagService(IOptionsMonitor<FeatureFlagSettings> options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public bool UseAzureAISearch => _options.CurrentValue.UseAzureAISearch;
    }
}
