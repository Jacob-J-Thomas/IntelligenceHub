using IntelligenceHub.API.DTOs.DataAccessDTOs;

namespace IntelligenceHub.API.DTOs.ClientDTOs.RagDTOs
{
    public class RagUpsertRequest
    {
        public string Index { get; set; }
        public List<RawRagDocument> Documents { get; set; } = new List<RawRagDocument>();
    }
}
