using OpenAICustomFunctionCallingAPI.Common.Extensions;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs
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
            foreach (var prop in properties) // need if statement here?
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
