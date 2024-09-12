using Newtonsoft.Json;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL.DTOs;

namespace IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs
{
    public class PropertyDTO
    {
        [JsonIgnore]
        public int? id { get; set; }
        public string type { get; set; }
        public string? description { get; set; }

        public PropertyDTO() { }

        public PropertyDTO(DbPropertyDTO property)
        {
            ConvertToAPIPropertyDTO(property);
        }

        public void ConvertToAPIPropertyDTO(DbPropertyDTO property)
        {
            id = property.Id;
            type = property.Type;
            description = property.Description;
        }
    }
}
