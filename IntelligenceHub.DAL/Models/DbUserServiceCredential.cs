using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    /// <summary>
    /// Represents service credentials supplied by a user for external AGI or RAG services.
    /// </summary>
    [Table("UserServiceCredentials")]
    public class DbUserServiceCredential
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary>
        /// Primary key for the credential entry.
        /// </summary>
        public int Id { get; set; }

        [Required]
        /// <summary>
        /// Authenticated user identifier associated with this credential.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        [Required]
        /// <summary>
        /// Specifies whether this credential is for AGI or RAG services.
        /// </summary>
        public string ServiceType { get; set; } = string.Empty;

        /// <summary>
        /// Optional service host/provider name.
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// Service endpoint URL provided by the user.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Base64 encoded API key for the service.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
    }
}
