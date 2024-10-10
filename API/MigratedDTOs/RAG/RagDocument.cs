using IntelligenceHub.Common.Attributes;

namespace IntelligenceHub.API.DTOs.DataAccessDTOs
{
    public class RagDocument
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Topic { get; set; }
        public string Keywords { get; set; }
        public string Source { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }
    }
}
