using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    /// <summary>
    /// Represents a Stripe subscription item for a specific user and usage type.
    /// </summary>
    [Table("UserSubscriptionItems")]
    public class DbSubscriptionItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string UsageType { get; set; } = string.Empty;

        [Required]
        public string SubscriptionItemId { get; set; } = string.Empty;
    }
}
