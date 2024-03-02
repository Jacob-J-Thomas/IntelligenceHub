using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using OpenAICustomFunctionCallingAPI.Common.Extensions;

namespace OpenAICustomFunctionCallingAPI.API.DTOs
{
    public class Tool
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public int Id { get; set; }
        //[JsonIgnore] // probably need to create a base class to handle this better
        public string Type { get; set; } = "function"; // this should always equal function
        public FunctionDTO Function { get; set; }

        public Tool() { }

        public Tool(DbToolDTO tool, List<DbPropertyDTO> properties)
        {
            Id = tool.Id;
            // need to set Type = default or something?
            Function = new FunctionDTO(tool, properties);
        }

        public Tool(DbToolDTO tool)
        {
            Id = tool.Id;
            // need to set Type = default or something?
            Function = new FunctionDTO(tool);
        }
    }

    public class FunctionDTO
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public ParametersDTO? Parameters { get; set; }

        public FunctionDTO() { }

        public FunctionDTO(DbToolDTO tool)
        {
            Name = tool.Name;
            Description = tool.Description;
        }

        public FunctionDTO(DbToolDTO tool, List<DbPropertyDTO> properties)
        {
            Name = tool.Name;
            Description = tool.Description;
            Parameters = new ParametersDTO(tool, properties);
        }
    }

    public class ParametersDTO
    {
        [JsonIgnore]
        public string Type { get; set; } = "object"; // this is always equal to object
        public Dictionary<string, PropertyDTO>? Properties { get; set; }
        public string[]? Required { get; set; }

        public ParametersDTO() { }


        public ParametersDTO(DbToolDTO tool, List<DbPropertyDTO> properties)
        {
            Properties = new Dictionary<string, PropertyDTO>();
            foreach (var prop in properties)
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

    public class PropertyDTO
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Type { get; set; }
        public string? Description { get; set; }
        public string[]? Enum { get; set; }

        public PropertyDTO() { }    

        public PropertyDTO(DbPropertyDTO property)
        {
            Id = property.Id;
            Type = property.Type;
            Description = property.Description;
            if (!string.IsNullOrEmpty(property.Enum))
            {
                Enum = property.Enum.ToStringArray();
            }
        }
    }
}
