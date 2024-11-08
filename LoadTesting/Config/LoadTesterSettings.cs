
namespace LoadTester.Config
{
    public class LoadTesterSettings
    {
        public string TargetUrl { get; set; }
        public string AuthEndpoint { get; set; }
        public string AuthClientId { get; set; }
        public string AuthClientSecret { get; set; }
        public string ElevatedAuthClientId { get; set; }
        public string ElevatedAuthClientSecret { get; set; }
        public string Audience { get; set; }
        public int TotalRequests { get; set; }
        public int ConcurrencyLevel { get; set; }
        public int ConcurrencyDelaySeconds { get; set; }
        public string ProfileName { get; set; }
        public string RagDatabase { get; set; }
        public string Completion {  get; set; }
    }
}
