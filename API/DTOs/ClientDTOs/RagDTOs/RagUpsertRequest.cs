using OpenAICustomFunctionCallingAPI.API.DTOs.DataAccessDTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.RagDTOs
{
    public class RagUpsertRequest
    {
        public string Index { get; set; }
        public List<RawRagDocument> Documents { get; set; } = new List<RawRagDocument>();
    }
}
