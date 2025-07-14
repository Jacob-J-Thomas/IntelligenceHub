namespace IntelligenceHub.Common.Config
{
    /// <summary>
    /// Configuration options for Stripe billing integration.
    /// </summary>
    public class StripeSettings
    {
        /// <summary>
        /// Gets or sets the Stripe API key used for requests.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
    }
}
