using OpenAICustomFunctionCallingAPI.API.DTOs.DataAccessDTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.RagDTOs
{
    public class RAGQueryResult
    {
        public string Title { get; set; }
        public List<RagChunk> Chunks { get; set; }
    }
}
