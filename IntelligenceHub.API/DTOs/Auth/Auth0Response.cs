using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntelligenceHub.API.DTOs.Auth
{
    /// <summary>
    /// Represents the response returned from the Auth0 authentication endpoint.
    /// </summary>
    public class Auth0Response
    {
        /// <summary>
        /// Gets or sets the generated access token.
        /// </summary>
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the lifetime of the access token in seconds.
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the type of the token that was issued.
        /// </summary>
        [JsonPropertyName("tokens_type")]
        public string TokenType { get; set; }
    }
}
