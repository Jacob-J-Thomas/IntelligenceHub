using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    /// <summary>
    /// Represents an authenticated user within the system.
    /// </summary>
    [Table("Users")]
    public class DbUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Auth0 subject identifier for the user.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Sub { get; set; } = string.Empty;

        /// <summary>
        /// Tenant identifier associated with the user.
        /// </summary>
        [Required]
        public Guid TenantId { get; set; }

        /// <summary>
        /// API token used for authentication requests.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ApiToken { get; set; } = string.Empty;
    }
}
