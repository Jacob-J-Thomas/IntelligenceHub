using System.Text.Json.Serialization;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response
{
    public class ResponseToolDTO
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public ResponseFunctionDTO? Function { get; set; }
    }
}