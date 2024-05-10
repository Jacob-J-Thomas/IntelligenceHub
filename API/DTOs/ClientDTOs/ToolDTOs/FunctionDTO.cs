using OpenAICustomFunctionCallingAPI.DAL.DTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs
{
    public class FunctionDTO
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public ParametersDTO? Parameters { get; set; }

        public FunctionDTO() 
        {
            SetParametersDefault();
        }

        public FunctionDTO(DbToolDTO tool, List<DbPropertyDTO> properties)
        {
            ConvertToAPIFunctionDTO(tool, properties);
        }

        public void ConvertToAPIFunctionDTO(DbToolDTO tool, List<DbPropertyDTO>? properties) 
        {
            Name = tool.Name;
            Description = tool.Description;
            if (properties != null && properties.Count > 0)
            {
                Parameters = new ParametersDTO(tool, properties);
            }
            else
            {
                SetParametersDefault();
            }
        }

        public void SetParametersDefault()
        {
            if (Parameters == null)
            {
                Parameters = new ParametersDTO();
            }
        }
    }
}
