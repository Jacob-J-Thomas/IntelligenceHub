using System.Text.Json.Serialization;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response
{
    public class ResponseChoiceDTO
    {
        public int? Index { get; set; }
        public ResponseMessageDTO? Message { get; set; }
        public LogprobsDTO? Logprobs { get; set; }
        public string? Finish_Reason { get; set; }
    }
}