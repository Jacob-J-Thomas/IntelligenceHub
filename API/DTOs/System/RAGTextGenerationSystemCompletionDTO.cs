using Azure.AI.OpenAI;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.MessageDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.RagDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.DataAccessDTOs;
using System.Reflection.Metadata;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.System
{
    public class RAGTextGenerationSystemCompletionDTO : DefaultCompletionDTO
    {
        public RAGTextGenerationSystemCompletionDTO(string dataFormat, string model, RagChunk chunk) 
        {
            var systemMessage = "You are part of an API that chunks documents for retrieval augmented " +
                "generation tasks. Your job is to take the requests, which are sent to you programatically, " +
                "and shorten the data into a topic, keywords, or another form of data. Please take care to " +
                "only provide the data requested in the completion, as any words unrelated to the completion " +
                "request will be interpreted as part of the topic or keyword.";

            var completion = $"Please create {dataFormat} summarizing the below data delimited by triple " +
                $"backticks. Your response should only contain {dataFormat} and absolutely no other textual " +
                $"data.\n\n";
            completion += "```"; // triple backticks to delimit the data
            completion += $"title: {chunk.Title}\n\ncontent: {chunk.Content}";
            completion += "\n```";

            Model = model;
            Messages = new List<MessageDTO>()
            {
                new MessageDTO() { Content = systemMessage, Role = "system" },
                new MessageDTO() { Content = completion, Role = "user" },
            };
        }
    }
}
