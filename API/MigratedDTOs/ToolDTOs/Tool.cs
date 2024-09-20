
using IntelligenceHub.DAL.DTOs;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using IntelligenceHub.Common.Extensions;
using System.Text.Json.Serialization;

namespace IntelligenceHub.API.MigratedDTOs.ToolDTOs
{
    public class Tool
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Type { get; private set; } = "function";
        public Function Function { get; set; } = new Function();
    }
}
