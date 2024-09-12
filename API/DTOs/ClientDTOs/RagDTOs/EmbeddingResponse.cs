using IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs.Response;

namespace IntelligenceHub.API.DTOs.ClientDTOs.EmbeddingDTOs
{
    public class EmbeddingResponse
    {
        public string Object { get; set; }
        public EmbeddingData[] Data { get; set; }
        public string Model { get; set; }
        public UsageDTO Usage { get; set; }

        public static explicit operator EmbeddingResponse(EmbeddingRequestBase v)
        {
            throw new NotImplementedException();
        }
    }
}
