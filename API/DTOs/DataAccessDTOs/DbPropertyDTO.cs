using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs;
using IntelligenceHub.Common.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.DTOs
{
    [TableName("Properties")]
    public class DbPropertyDTO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public int ToolId { get; set; }

        public DbPropertyDTO() { }

        public DbPropertyDTO(string propertyName, PropertyDTO dto)
        {
            ConvertToDbPropertyDTO(propertyName, dto);
        }

        public void ConvertToDbPropertyDTO(string propertyName, PropertyDTO dto)
        {
            Id = dto.id ?? 0;
            ToolId = dto.id ?? 0;
            Name = propertyName;
            Type = dto.type;
            if (dto.description != null)
            {
                Description = dto.description;
            }
        }
    }
}
