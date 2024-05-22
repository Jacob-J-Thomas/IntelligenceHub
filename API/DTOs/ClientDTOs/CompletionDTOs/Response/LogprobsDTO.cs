using System.Text.Json.Serialization;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response
{
    public class LogprobsDTO
    {
        public List<ContentLogprobsDTO>? Content { get; set; }
    }
}