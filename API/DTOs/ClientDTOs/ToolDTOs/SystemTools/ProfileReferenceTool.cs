using Nest;
using IntelligenceHub.DAL.DTOs;
using IntelligenceHub.API.MigratedDTOs.ToolDTOs;

namespace IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs.SystemTools
{
    public class ProfileReferenceTools : Tool
    {
        public ProfileReferenceTools(DbProfile profile)
        {
            var dialogueHistoryProperty = new Property()
            {
                Type = "string",
                Description = "The response you have, to the prompt you recieved, particularly as it concerns augmenting" +
                                "the response of the large language model which you are requesting a recursive completion from," +
                                "or answering a user query. This data will be appended as if it were the last message sent in the " +
                                "conversation."
            };

            Function = new Function()
            {
                Name = profile.Name + "_Reference_AI_Model", // add this to the criteria for validating tools
                Description = "A call to a large language model that can be used to generate recursive chat completions between other AI models." +
                              $"Here is a description of this particular AI model configuration: {profile.Reference_Description}",
            };

            Function.Parameters.Properties.Add("prompt_response", dialogueHistoryProperty);
        }
    }
}
