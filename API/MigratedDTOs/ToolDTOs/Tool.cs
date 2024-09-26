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
