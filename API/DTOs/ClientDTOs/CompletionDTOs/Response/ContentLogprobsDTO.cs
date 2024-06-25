using System.Text.Json.Serialization;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response
{
    public class ContentLogprobsDTO
    {
        public string? Token { get; set; }
        public double? Logprob { get; set; }
        public List<byte>? Bytes { get; set; }
        public List<TokenLogprobDTO>? TopLogprobs { get; set; }
    }
}