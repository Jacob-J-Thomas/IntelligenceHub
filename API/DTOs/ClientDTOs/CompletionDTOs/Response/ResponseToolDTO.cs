//using Azure.AI.OpenAI;
//using System.Text.Json.Serialization;

//namespace IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs.Response
//{
//    public class ResponseToolDTO
//    {
//        public string? Id { get; set; }
//        public string? Type { get; set; }
//        public ResponseFunctionDTO? Function { get; set; }

//        public ResponseToolDTO() { }

//        public ResponseToolDTO(StreamingFunctionToolCallUpdate toolCall)
//        {
//            BuildFromStream(toolCall);
//        }

//        public void BuildFromStream(StreamingFunctionToolCallUpdate toolCall)
//        {
//            Id = toolCall.Id;
//            Function = new ResponseFunctionDTO()
//            {
//                Name = toolCall.Name
//            };
//        }
//    }
//}