using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Client.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Implementations
{
    public class AGIClientFactory : IAGIClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AGIClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IAGIClient GetClient(string modelName)
        {
            modelName = modelName.ToLower(); 

            if (modelName == AGIServiceHosts.OpenAI.ToString().ToLower()) return _serviceProvider.GetRequiredService<OpenAIClient>();
            else if (modelName == AGIServiceHosts.Azure.ToString().ToLower()) return _serviceProvider.GetRequiredService<OpenAIClient>();
            else if (modelName == AGIServiceHosts.Anthropic.ToString().ToLower()) return _serviceProvider.GetRequiredService<AnthropicAIClient>();
            else if (modelName == AGIServiceHosts.Groq.ToString().ToLower()) return _serviceProvider.GetRequiredService<GroqAIClient>();

            // Handle Errors
            else throw new ArgumentException($"Invalid model name: {modelName}");
        }
    }
}