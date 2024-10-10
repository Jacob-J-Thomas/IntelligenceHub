namespace IntelligenceHub.API.DTOs.RAG
{
    public class RagUpsertRequest
    {
        public List<RagDocument> Documents { get; set; } = new List<RagDocument>();
    }
}
