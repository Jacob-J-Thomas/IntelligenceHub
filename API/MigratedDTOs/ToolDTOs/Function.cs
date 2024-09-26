
namespace IntelligenceHub.API.MigratedDTOs.ToolDTOs
{
    public class Function
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public Parameters Parameters { get; set; } = new Parameters();
    }
}
