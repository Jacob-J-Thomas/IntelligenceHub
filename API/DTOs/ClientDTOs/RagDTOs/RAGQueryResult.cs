using IntelligenceHub.API.DTOs.DataAccessDTOs;

namespace IntelligenceHub.API.DTOs.ClientDTOs.RagDTOs
{
    public class RAGQueryResult
    {
        public string Title { get; set; }
        public List<RagChunk> Chunks { get; set; }
    }
}
