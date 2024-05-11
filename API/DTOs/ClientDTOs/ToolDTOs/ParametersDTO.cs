using OpenAICustomFunctionCallingAPI.Common.Extensions;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs
{
    public class ParametersDTO
    {
        public string Type { get; private set; } = "object"; 
        public Dictionary<string, PropertyDTO>? Properties { get; set; } = new Dictionary<string, PropertyDTO>();
        public string[]? Required { get; set; } = new string[] { };

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
                Properties.Add(prop.Name, propDto);
            }
            if (tool.Required != null)
            {
                Required = tool.Required.ToStringArray();
            }
        }
    }
}
