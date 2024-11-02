using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.API.DTOs.Tools
{
    public class ProfileReferenceTools : Tool
    {
        public ProfileReferenceTools(DbProfile profile)
        {
            var dialogueHistoryProperty = new Property()
            {
                Type = "string",
                Description = "The response you have, to the prompt you recieved, particularly the data that will concern the next" +
                              "large language model which you are requesting a recursive completion from, or in order to answer/complete " +
                              "the original user's prompt. This data will be appended as the last message sent in the conversation, and you " +
                              "should write it as such."
            };

            Function = new Function()
            {
                Name = profile.Name + "_Reference_AI_Model", // add this to the criteria for validating tools
                Description = $"A call to a large language model that can be used to generate recursive chat completions between other AI models, " +
                              $"and yourself. Here is a description of this particular AI model configuration: {profile.ReferenceDescription}",
            };

            Function.Parameters.Properties.Add("prompt_response", dialogueHistoryProperty);
        }
    }
}
