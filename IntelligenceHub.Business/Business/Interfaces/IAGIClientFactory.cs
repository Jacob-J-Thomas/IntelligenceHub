using IntelligenceHub.Client.Interfaces;

namespace IntelligenceHub.Business.Interfaces
{
    public interface IAGIClientFactory
    {
        IAGIClient GetClient(string modelName);
    }
}
