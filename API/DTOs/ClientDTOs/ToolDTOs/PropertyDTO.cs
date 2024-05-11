using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.Common.Extensions;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs
{
    public class PropertyDTO
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public string Type { get; set; }
        public string? Description { get; set; }

        public PropertyDTO() { }

        public PropertyDTO(DbPropertyDTO property)
        {
            ConvertToAPIPropertyDTO(property);
        }

        public void ConvertToAPIPropertyDTO(DbPropertyDTO property)
        {
            Id = property.Id;
            Type = property.Type;
            Description = property.Description;
        }
    }
}
