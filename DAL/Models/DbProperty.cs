using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs;
using IntelligenceHub.Common.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.DTOs
{
    [TableName("Properties")]
    public class DbProperty
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public int ToolId { get; set; }

        public DbProperty() { }

        public DbProperty(string propertyName, Property dto)
        {
            ConvertToDbPropertyDTO(propertyName, dto);
        }

        public void ConvertToDbPropertyDTO(string propertyName, Property dto)
        {
            Id = dto.Id ?? 0;
            ToolId = dto.Id ?? 0;
            Name = propertyName;
            Type = dto.Type;
            if (dto.Description != null)
            {
                Description = dto.Description;
            }
        }
    }
}
