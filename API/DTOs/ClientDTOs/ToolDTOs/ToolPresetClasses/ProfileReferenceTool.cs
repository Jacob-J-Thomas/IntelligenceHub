using Nest;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs.ToolPresetClasses
{
    public class ProfileReferenceTools : ToolDTO
    {
        public ProfileReferenceTools(DbProfileDTO profile)
        {
            var dialogueHistoryProperty = new PropertyDTO()
            {
                type = "string",
                description = "The response you have, to the prompt you recieved, particularly as it concerns augmenting" +
                                "the response of the large language model which you are requesting a recursive completion from," +
                                "or answering a user query. This data will be appended as if it were the last message sent in the " +
                                "conversation."
            };

            //var returnResponse = new PropertyDTO()
            //{
            //    Type = "boolean",
            //    Description = "A boolean value of 'true' or 'false' indicating whether or not your completion should be returned " +
            //                  "immediately to provide data to the end user while the system is still processing, or not. This should" +
            //                  "only be true if you provided a value for prompt_response."
            //};

            Function = new FunctionDTO()
            {
                Name = profile.Name + "_Reference_AI_Model", // add this to the criteria for validating tools
                Description = "A call to a large language model that can be used to generate recursive chat completions between other AI models." +
                              $"Here is a description of this particular AI model configuration: {profile.Reference_Description}",
            };

            Function.Parameters.properties.Add("prompt_response", dialogueHistoryProperty);
            //Function.Parameters.Properties.Add("return_response", returnResponse);
        }
    }
}
