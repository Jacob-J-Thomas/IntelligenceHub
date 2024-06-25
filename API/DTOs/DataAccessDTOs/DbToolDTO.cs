using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs;
using OpenAICustomFunctionCallingAPI.Common.Attributes;
using OpenAICustomFunctionCallingAPI.Common.Extensions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAICustomFunctionCallingAPI.DAL.DTOs
{
    [TableName("Tools")]
    public class DbToolDTO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public string Required { get; set; }

        public DbToolDTO() { }

        public DbToolDTO(ToolDTO tool) : base()
        {
            ConvertToDbTool(tool);
        }

        public void ConvertToDbTool(ToolDTO tool)
        {
            Id = tool.Id;
            Name = tool.Function.Name;
            if (tool.Function != null && tool.Function.Description != null)
            {
                Description = tool.Function.Description;
            }
            if (tool.Function != null && tool.Function.Parameters != null && tool.Function.Parameters.required != null && tool.Function.Parameters.required.Length > 0)
            {
                Required = tool.Function.Parameters.required.ToCommaSeparatedString();
            }
        }
    }
}
