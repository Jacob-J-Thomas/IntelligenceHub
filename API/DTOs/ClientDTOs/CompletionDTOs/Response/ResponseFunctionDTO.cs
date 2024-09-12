using System.Text.Json.Serialization;

namespace IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs.Response
{
    public class ResponseFunctionDTO
    {
        public string? Name { get; set; }
        public string? Arguments { get; set; }
    }
}