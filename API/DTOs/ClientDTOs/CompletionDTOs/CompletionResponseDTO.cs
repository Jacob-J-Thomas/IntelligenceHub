using System.Text.Json.Serialization;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs
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

    public class TokenLogprobDTO
    {
        public string? Token { get; set; }
        public double? Logprob { get; set; }
        public List<byte>? Bytes { get; set; }
    }

    public class ContentLogprobsDTO
    {
        public string? Token { get; set; }
        public double? Logprob { get; set; }
        public List<byte>? Bytes { get; set; }
        public List<TokenLogprobDTO>? TopLogprobs { get; set; }
    }

    public class LogprobsDTO
    {
        public List<ContentLogprobsDTO>? Content { get; set; }
    }

    public class ResponseFunctionDTO
    {
        public string? Name { get; set; }
        public string? Arguments { get; set; }
    }

    public class ResponseToolDTO
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public ResponseFunctionDTO? Function { get; set; }
    }

    public class ResponseMessageDTO
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
        public List<ResponseToolDTO>? Tool_Calls { get; set; }
    }

    public class ResponseChoiceDTO
    {
        public int? Index { get; set; }
        public ResponseMessageDTO? Message { get; set; }
        public LogprobsDTO? Logprobs { get; set; }
        public string? Finish_Reason { get; set; }
    }

    public class UsageDTO
    {
        public int? Prompt_Tokens { get; set; }
        public int? Completion_Tokens { get; set; }
        public int? Total_Tokens { get; set; }
    }
}