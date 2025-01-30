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
            modelName = modelName.Remove('-').ToLower(); // enums do not support dashes
            return modelName switch
            {
                // OpenAI Models
                ValidAIModels.Gpt4o.ToString().ToLower() => _serviceProvider.GetRequiredService<OpenAIClient>(),
                ValidAIModels.Gpt4omini.ToString().ToLower() => _serviceProvider.GetRequiredService<OpenAIClient>(),

                // Azure OpenAI Models
                ValidAIModels.AzureGpt4o.ToString().ToLower() => _serviceProvider.GetRequiredService<AzureOpenAIClient>(),
                ValidAIModels.AzureGpt4omini.ToString().ToLower() => _serviceProvider.GetRequiredService<AzureOpenAIClient>(),

                // Groq Models
                ValidAIModels.Llama.ToString().ToLower() => _serviceProvider.GetRequiredService<GroqClient>(),
                ValidAIModels.Gemma.ToString().ToLower() => _serviceProvider.GetRequiredService<GroqClient>(),
                ValidAIModels.Mixtral.ToString().ToLower() => _serviceProvider.GetRequiredService<GroqClient>(),

                // Anthropic Models
                ValidAIModels.Claude.ToString().ToLower() => _serviceProvider.GetRequiredService<AnthropicClient>(),

                // Bedrock
                ValidAIModels.Bedrock.ToString().ToLower() => _serviceProvider.GetRequiredService<BedrockClient>(),
            };
        }
    }
}