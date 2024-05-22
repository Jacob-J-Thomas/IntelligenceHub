using System.Text.Json.Serialization;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response
{
    public class UsageDTO
    {
        public int? Prompt_Tokens { get; set; }
        public int? Completion_Tokens { get; set; }
        public int? Total_Tokens { get; set; }
    }
}