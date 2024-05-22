using System.Text.Json.Serialization;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response
{
    public class ResponseMessageDTO
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
        public List<ResponseToolDTO>? Tool_Calls { get; set; }
    }
}