using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.MessageDTOs;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs
{
    // A standardized chat completion DTO based off of OpenAI's API. Several other
    // companies have adopted OpenAI's json structures to preserve functionality
    // accross services.
    public class StandardCompletionDTO : BaseCompletionDTO
    {
        [JsonIgnore]
        public override string System_Message { get; set; }
        [JsonIgnore]
        public override string Reference_Description { get; set; }
        [JsonIgnore]
        public override bool? Return_Recursion { get; set; }
        public List<MessageDTO> Messages { get; set; } = new List<MessageDTO>();

        public StandardCompletionDTO(APIProfileDTO completion) : base(completion)
        {

        }

        public StandardCompletionDTO(APIProfileDTO completion, BaseCompletionDTO? modifiers) : base(completion, modifiers) 
        {
            
        }
    }
}
