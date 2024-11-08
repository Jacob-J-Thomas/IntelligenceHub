using IntelligenceHub.API.DTOs;
using LoadTester.Auth;
using LoadTester.Config;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace LoadTester
{
    class Program
    {
        private static readonly HttpClient _client = new HttpClient();
        private static int _requestCount = 0;
        private static int _successCount = 0;
        private static int _failureCount = 0;
        private static double _avgRequestSeconds;
        private static double _longestRequestSeconds;

        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var settings = configuration.GetRequiredSection(nameof(LoadTesterSettings)).Get<LoadTesterSettings>()
                ?? throw new ArgumentNullException("Appsettings failed to be configured...");

            // validation checks
            if (settings.TotalRequests < settings.ConcurrencyLevel) throw new ArgumentException("The number of requests must be greater than the concurrency level.");

            // Set authentication headers
            var authClient = new AuthClient(settings);
            var authResponse = await authClient.RequestAuthToken();
            if (string.IsNullOrEmpty(authResponse?.AccessToken)) throw new InvalidOperationException("The auth token failed to be retrieved. Testing could not continue...");
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authResponse.AccessToken}");

            // Could modify this method to 
            var request = BuildRequest(settings);

            // Calculate the number of batches needed  
            var batches = (int)Math.Ceiling((double)settings.TotalRequests / settings.ConcurrencyLevel);

            // Begin testing
            var tasks = new List<Task>();
            Console.WriteLine("Beggining stress test...\n");
            for (var batch = 0; batch < batches; batch++)
            {
                // Determine how many requests this batch should send  
                var requestsInThisBatch = (batch == batches - 1) ? settings.TotalRequests - (batch * settings.ConcurrencyLevel) : settings.ConcurrencyLevel;

                // Create tasks for the current batch  
                for (var i = 0; i < requestsInThisBatch; i++)
                {
                    tasks.Add(Task.Run(() => SendRequests(settings, request)));
                }

                // Wait for all tasks in the current batch to complete  
                await Task.WhenAll(tasks);

                Console.WriteLine($"Batch {batch + 1}/{batches} completed.");
                Console.WriteLine($"Total Requests: {_requestCount}");
                Console.WriteLine($"Successful Requests: {_successCount}");
                Console.WriteLine($"Failed Requests: {_failureCount}");
                Console.WriteLine($"Longest Request Time: {_longestRequestSeconds}");
                Console.WriteLine($"Average Request Time: {_avgRequestSeconds}");

                // Clear the task list for the next batch  
                tasks.Clear();

                // Delay before starting the next batch, if not the last batch  
                if (batch < batches - 1)
                {
                    Console.WriteLine($"\nWaiting for {settings.ConcurrencyDelaySeconds} seconds before next batch...\n");
                    await Task.Delay(settings.ConcurrencyDelaySeconds * 1000);// convert seconds to miliseconds
                }
            }
            Console.WriteLine("\nStress test completed.\n");
        }

        private static CompletionRequest BuildRequest(LoadTesterSettings settings)
        {
            return new CompletionRequest()
            {
                Messages = new List<Message>() { new Message() { Role = IntelligenceHub.Common.GlobalVariables.Role.User, Content = settings.Completion } }
            };
        }

        private static async Task SendRequests(LoadTesterSettings settings, CompletionRequest request)
        {
            Interlocked.Increment(ref _requestCount);
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
                var sentTime = DateTime.UtcNow;
                var response = await _client.PostAsync($"{settings.TargetUrl}/{settings.ProfileName}", requestContent);

                // Calculate details for longest request time
                var processingTime = DateTime.UtcNow - sentTime;
                _longestRequestSeconds = _longestRequestSeconds == 0 ? processingTime.TotalMilliseconds / 1000 : _longestRequestSeconds;
                if (_longestRequestSeconds < processingTime.Seconds) _longestRequestSeconds = processingTime.TotalMilliseconds / 1000;

                // Calculate details for average request time
                _avgRequestSeconds = _avgRequestSeconds == 0 ? processingTime.TotalMilliseconds / 1000 : _avgRequestSeconds;

                // The below formula is a derivation of a standard average calculation. Here, we don't have access
                // to the previous response times, so instead of suming these values, we multiply them by the amount
                // of requests minus the current request. This allows us to calculate what the sum of all the responses
                // would have been, and then add our latest time to this sum.  We then divide by the total amount of
                // requests like normal, and this gives us the new average.
                _avgRequestSeconds = ((_avgRequestSeconds * (_requestCount - 1)) + (processingTime.TotalMilliseconds / 1000)) / _requestCount;

                // extract response message
                var responseString = await response.Content.ReadAsStringAsync();
                CompletionResponse completionData = JsonConvert.DeserializeObject<CompletionResponse>(responseString);
                var messageData = completionData?.Messages[0];

                var responseMessage = messageData?.Content;

                // ensure response did not include an error message
                if (response.IsSuccessStatusCode || (completionData?.FinishReason != null && completionData.FinishReason.HasValue 
                    && completionData.FinishReason.Value != IntelligenceHub.Common.GlobalVariables.FinishReason.Error)) 
                    Interlocked.Increment(ref _successCount);
                else 
                    Interlocked.Increment(ref _failureCount);
            }
            catch
            {
                Interlocked.Increment(ref _failureCount);
            }
        }
    }
}