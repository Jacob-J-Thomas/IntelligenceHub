using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.EmbeddingDTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ControllerDTOs
{
    public class DirectQueryRequest : EmbeddingRequestBase
    {
        //public string[] QueryTargets { get; set; }
        public string QueryTarget { get; set; }
        public int DocNum { get; set; }
    }
}
