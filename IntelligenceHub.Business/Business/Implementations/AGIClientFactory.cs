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
            modelName = modelName.Replace("-", "").ToLower(); // enums do not support dashes

            // OpenAI Models
            if (modelName == ValidAIModels.Gpt4o.ToString().ToLower()) return _serviceProvider.GetRequiredService<OpenAIClient>();
            else if (modelName == ValidAIModels.Gpt4omini.ToString().ToLower()) return _serviceProvider.GetRequiredService<OpenAIClient>();

            // Azure OpenAI Models
            else if (modelName == ValidAIModels.AzureGpt4o.ToString().ToLower()) return _serviceProvider.GetRequiredService<AzureOpenAIClient>();
            else if (modelName == ValidAIModels.AzureGpt4oMini.ToString().ToLower()) return _serviceProvider.GetRequiredService<AzureOpenAIClient>();

            // Groq Models
            else if (modelName == ValidAIModels.Gemma.ToString().ToLower()) return _serviceProvider.GetRequiredService<GroqClient>();
            else if (modelName == ValidAIModels.Llama.ToString().ToLower()) return _serviceProvider.GetRequiredService<GroqClient>();
            else if (modelName == ValidAIModels.Mixtral.ToString().ToLower()) return _serviceProvider.GetRequiredService<GroqClient>();

            // Anthropic Models
            else if (modelName == ValidAIModels.Claude.ToString().ToLower()) return _serviceProvider.GetRequiredService<AnthropicClient>();

            // Handle Errors
            else throw new ArgumentException($"Invalid model name: {modelName}");
        }
    }
}