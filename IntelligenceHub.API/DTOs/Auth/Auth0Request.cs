using System.Text.Json.Serialization;

namespace IntelligenceHub.API.DTOs.Auth
{
    /// <summary>
    /// Represents the request body sent to Auth0 to obtain an access token.
    /// </summary>
    public class Auth0Request
    {
        /// <summary>
        /// Gets or sets the OAuth grant type being requested.
        /// </summary>
        [JsonPropertyName("grant_type")]
        public string GrantType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Auth0 client identifier.
        /// </summary>
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Auth0 client secret.
        /// </summary>
        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API audience for which the token is requested.
        /// </summary>
        [JsonPropertyName("audience")]
        public string Audience { get; set; } = string.Empty;
    }
}
