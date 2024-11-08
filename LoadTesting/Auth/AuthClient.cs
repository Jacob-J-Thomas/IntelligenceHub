using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using LoadTester.Config;

namespace LoadTester.Auth
{
    public class AuthClient
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly AuthRequest _request;
        private readonly AuthRequest _elevatedRequest;

        private readonly string _authEndpoint;

        public AuthClient(LoadTesterSettings settings)
        {
            _authEndpoint = settings.AuthEndpoint;

            _request = new AuthRequest()
            {
                GrantType = "client_credentials",
                ClientId = settings.AuthClientId,
                ClientSecret = settings.AuthClientSecret,
                Audience = settings.Audience,
            };

            _elevatedRequest = new AuthRequest()
            {
                GrantType = "client_credentials",
                ClientId = settings.ElevatedAuthClientId,
                ClientSecret = settings.ElevatedAuthClientSecret,
                Audience = settings.Audience,
            };
        }

        // Returns a basic token that is safe to share with front end clients
        public async Task<AuthResponse?> RequestAuthToken()
        {
            var json = JsonSerializer.Serialize(_request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = await _client.PostAsync(_authEndpoint, content);

                var jsonString = await response.Content.ReadAsStringAsync();

                var authToken = JsonSerializer.Deserialize<AuthResponse>(jsonString, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                return authToken ?? null;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // The token returned from the below method should not be shared with the front end
        public async Task<AuthResponse?> RequestElevatedAuthToken()
        {
            var json = JsonSerializer.Serialize(_elevatedRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = await _client.PostAsync(_authEndpoint, content);

                var jsonString = await response.Content.ReadAsStringAsync();

                var authToken = JsonSerializer.Deserialize<AuthResponse>(jsonString, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                return authToken ?? null;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
