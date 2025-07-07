using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Client.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Factories
{
    /// <summary>
    /// Factory for creating RAG search service clients.
    /// </summary>
    public class RagClientFactory : IRagClientFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public RagClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public IAISearchServiceClient GetClient(VectorDbProvider host)
        {
            if (host == VectorDbProvider.Weaviate)
                return _serviceProvider.GetRequiredService<WeaviateSearchServiceClient>();
            return _serviceProvider.GetRequiredService<AISearchServiceClient>();
        }
    }
}
