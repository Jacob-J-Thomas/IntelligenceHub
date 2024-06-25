namespace OpenAICustomFunctionCallingAPI.API.DTOs.DataAccessDTOs
{
    public class ChunkedRagDocument : RagDocumentBase
    {
        public List<RagChunk> Chunks { get; set; } = new List<RagChunk>();
    }
}
