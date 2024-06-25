using System.Text.Json.Serialization;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response
{
    // folder structure and inheritence should be changed to accomadate for this being used by multiple other DTOs
    public class UsageDTO
    {
        public int? Prompt_Tokens { get; set; }
        public int? Completion_Tokens { get; set; }
        public int? Total_Tokens { get; set; }
    }
}