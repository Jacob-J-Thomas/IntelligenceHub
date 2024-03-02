using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.Common;
using OpenAICustomFunctionCallingAPI.Common.Attributes;
using OpenAICustomFunctionCallingAPI.Common.Extensions;
using OpenAICustomFunctionCallingAPI.DAL;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAICustomFunctionCallingAPI.DAL.DTOs
{
    // extend this from a common DTO?
    [TableName("Tools")]
    public class DbToolDTO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore] // shouldn't be needed
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Type { get; set; }
        //public string Properties { get; set; }
        public string Required { get; set; }

        public DbToolDTO() { }

        public DbToolDTO(Tool tool)
        {
            Id = tool.Id;
            Name = tool.Function.Name;
            Description = tool.Function.Description;
            Type = tool.Type;
            if (tool.Function != null && tool.Function.Parameters != null && tool.Function.Parameters.Required != null && tool.Function.Parameters.Required.Length > 0)
            {
                Required = tool.Function.Parameters.Required.ToCommaSeparatedString();
            }

            //Properties = new List<DbPropertyDTO>();
            //foreach(var prop in tool.Function.Parameters.Properties)
            //{
            //    Properties.Add(new DbPropertyDTO(prop.Key, prop.Value));
            //}
        }
    }
}
