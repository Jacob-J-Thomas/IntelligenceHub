
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.MigratedDTOs
{
    public class Message
    {
        public Role Role { get; set; }
        public string Content { get; set; }
        public string? ToolCallID { get; set; }
    }
}
