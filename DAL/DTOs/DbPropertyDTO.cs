using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.Common.Attributes;
using OpenAICustomFunctionCallingAPI.Common.Extensions;
using OpenAICustomFunctionCallingAPI.DAL;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAICustomFunctionCallingAPI.DAL.DTOs
{
    [TableName("Properties")]
    public class DbPropertyDTO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; } // do we even need this?
        public string Type { get; set; } = "function";
        public string? Description { get; set; }
        public string? Enum { get; set; }
        public int? ToolId { get; set; }

        public DbPropertyDTO() { }

        // it would probably make more sense to have this declared
        public DbPropertyDTO(string propertyName, PropertyDTO dto)
        {
            Id = dto.Id;
            Name = propertyName;
            Type = dto.Type;
            Description = dto.Description;
            if (dto.Enum != null)
            {
                Enum = dto.Enum.ToCommaSeparatedString();
            }
        }

        public DbPropertyDTO(int toolId, string propertyName, PropertyDTO dto)
        {
            Id = dto.Id;
            Name = propertyName;
            Type = dto.Type;
            Description = dto.Description;
            ToolId = toolId;
            if (dto.Enum != null)
            {
                Enum = dto.Enum.ToCommaSeparatedString();
            }
        }
    }
}
