namespace IntelligenceHub.Host.Config
{
    public class Settings
    {
        public string AIEndpoint { get; set; }
        public string AIKey { get; set; }
        public string SearchServiceEndpoint { get; set; }
        public string SearchServiceKey { get; set; }
        public string DefaultEmbeddingModel { get; set; }
        public string DbConnectionString { get; set; }
        public string RagDbConnectionString { get; set; }
        public string PlaceholderConnectionString { get; set; }
    }
}
