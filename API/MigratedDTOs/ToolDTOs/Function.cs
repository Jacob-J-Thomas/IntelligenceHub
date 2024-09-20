using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs;
using IntelligenceHub.DAL.DTOs;

namespace IntelligenceHub.API.MigratedDTOs.ToolDTOs
{
    public class Function
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public Parameters Parameters { get; set; } = new Parameters();
    }
}
