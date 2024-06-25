using System.Text.Json.Serialization;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response
{
    public class TokenLogprobDTO
    {
        public string? Token { get; set; }
        public double? Logprob { get; set; }
        public List<byte>? Bytes { get; set; }
    }
}