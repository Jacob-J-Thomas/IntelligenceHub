
using Newtonsoft.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    public class Message
    {
        public Role? Role { get; set; }
        [JsonIgnore] // if this isn't working it might be because we are utilizing newtonsoft.json instead of system.text.json
        public string User { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Base64Image { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
