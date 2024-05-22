using System.Text.Json.Serialization;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response
{
    public class CompletionResponseDTO
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public int? Created { get; set; }
        public string? Model { get; set; }
        public List<ResponseChoiceDTO>? Choices { get; set; }
        public UsageDTO? Usage { get; set; }
        public string? System_Fingerprint { get; set; } // Marked as nullable since it can be null in JSON
    }
}