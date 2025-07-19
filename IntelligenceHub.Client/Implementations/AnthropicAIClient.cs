using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.Client.Implementations
{
    /// <summary>
    /// Thin wrapper around <see cref="AzureAIClient"/> for Anthropic models deployed via Azure Foundry.
    /// </summary>
    public class AnthropicAIClient : AzureAIClient
    {
        public AnthropicAIClient(IOptionsMonitor<AGIClientSettings> settings, IHttpClientFactory policyFactory)
            : base(settings, policyFactory, useAnthropic: true)
        {
        }
    }
}
