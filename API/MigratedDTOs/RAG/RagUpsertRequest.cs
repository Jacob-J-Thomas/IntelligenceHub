using IntelligenceHub.API.DTOs.DataAccessDTOs;

namespace OpenAICustomFunctionCallingAPI.API.MigratedDTOs.RAG
{
    public class RagUpsertRequest
    {
        public List<RagDocument> Documents { get; set; } = new List<RagDocument>();
    }
}
