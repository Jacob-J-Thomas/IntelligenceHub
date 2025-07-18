using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    /// <summary>
    /// Entity model representing a tool property in the database.
    /// </summary>
    [Table("Properties")]
    public class DbProperty : IntelligenceHub.DAL.Interfaces.ITenantEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary>
        /// Primary key for the property.
        /// </summary>
        public int Id { get; set; }
        [Required]
        /// <summary>
        /// Tenant identifier for the owning organization.
        /// </summary>
        public Guid TenantId { get; set; }
        /// <summary>
        /// Name of the property.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Data type of the property.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Description of the property.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Foreign key to the owning tool.
        /// </summary>
        public int ToolId { get; set; }
        [ForeignKey(nameof(ToolId))]
        /// <summary>
        /// Navigation property to the owning tool.
        /// </summary>
        public DbTool Tool { get; set; } = null!;
    }
}
