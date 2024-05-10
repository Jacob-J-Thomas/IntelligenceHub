using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.MessageDTOs;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs
{
    public class OpenAICompletionDTO : BaseCompletionDTO
    {
        [JsonIgnore]
        public override string System_Message { get; set; }
        [JsonIgnore]
        public override string Reference_Description { get; set; }
        [JsonIgnore]
        public override bool? Return_Recursion { get; set; }
        public List<MessageDTO> Messages { get; set; } = new List<MessageDTO>();

        public OpenAICompletionDTO(APIProfileDTO completion) : base(completion)
        {

        }

        public OpenAICompletionDTO(APIProfileDTO completion, BaseCompletionDTO? modifiers) : base(completion, modifiers) 
        {
            
        }
    }
}
