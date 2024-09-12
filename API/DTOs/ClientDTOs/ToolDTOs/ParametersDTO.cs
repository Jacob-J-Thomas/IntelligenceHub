using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL.DTOs;

namespace IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs
{
    public class ParametersDTO
    {
        public string type { get; private set; } = "object"; 
        public Dictionary<string, PropertyDTO>? properties { get; set; } = new Dictionary<string, PropertyDTO>();
        public string[]? required { get; set; } = new string[] { };

        public ParametersDTO() { }

        public ParametersDTO(DbToolDTO tool, List<DbPropertyDTO> properties)
        {
            ConvertToAPIParametersDTO(tool, properties);
        }

        public void ConvertToAPIParametersDTO(DbToolDTO tool, List<DbPropertyDTO> properties)
        {
            foreach (var prop in properties)
            {
                var propDto = new PropertyDTO(prop);
                this.properties.Add(prop.Name, propDto);
            }
            if (tool.Required != null)
            {
                required = tool.Required.ToStringArray();
            }
        }
    }
}
