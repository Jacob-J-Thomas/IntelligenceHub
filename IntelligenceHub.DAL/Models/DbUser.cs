using System;
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

        /// <summary>
        /// The user's subscription access level.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string AccessLevel { get; set; } = IntelligenceHub.Common.GlobalVariables.AccessLevel.Free.ToString();

        /// <summary>
        /// The number of completion requests made in the current month.
        /// </summary>
        public int RequestsThisMonth { get; set; }

        /// <summary>
        /// The start date of the request counting period.
        /// </summary>
        public DateTime RequestMonthStart { get; set; } = DateTime.UtcNow;
    }
}
