namespace IntelligenceHub.Common.Config
{
    /// <summary>
    /// Represents configuration options used for authentication.
    /// </summary>
    public class AuthSettings
    {
        /// <summary>
        /// Gets or sets the Auth0 domain.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the audience for issued tokens.
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Gets or sets the username for basic authentication.
        /// </summary>
        public string BasicUsername { get; set; }

        /// <summary>
        /// Gets or sets the password for basic authentication.
        /// </summary>
        public string BasicPassword { get; set; }

        /// <summary>
        /// Gets or sets the default Auth0 client identifier.
        /// </summary>
        public string DefaultClientId { get; set; }

        /// <summary>
        /// Gets or sets the default Auth0 client secret.
        /// </summary>
        public string DefaultClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the administrator client identifier.
        /// </summary>
        public string AdminClientId { get; set; }

        /// <summary>
        /// Gets or sets the administrator client secret.
        /// </summary>
        public string AdminClientSecret { get; set; }
    }
}

