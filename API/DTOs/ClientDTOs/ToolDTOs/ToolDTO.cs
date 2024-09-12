
using IntelligenceHub.DAL.DTOs;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using IntelligenceHub.Common.Extensions;
using System.Text.Json.Serialization;

namespace IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs
{
    public class ToolDTO
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Type { get; private set; } = "function";
        public FunctionDTO Function { get; set; } = new FunctionDTO();

        public ToolDTO() { }

        public ToolDTO(DbToolDTO tool, List<DbPropertyDTO> properties)
        {
            ConvertToAPIToolDTO(tool, properties);
        }

        public void ConvertToAPIToolDTO(DbToolDTO tool, List<DbPropertyDTO> properties)
        {
            Id = tool.Id;
            Function = new FunctionDTO(tool, properties);
        }
    }
}
