using Nest;
using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs;
using System.ComponentModel;

namespace OpenAICustomFunctionCallingAPI.API.DTOs
{
    public class ChatRequestDTO
    {
        public Guid? ConversationId { get; set; }
        public string ProfileName { get; set; }
        public string Completion { get; set; } // change to dictionary?
        [DefaultValue(0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int? MaxMessageHistory { get; set; }
        public RagRequestData? RagData { get; set; }
        public BaseCompletionDTO? ProfileModifiers { get; set; }

        public ChatRequestDTO() 
        {
        }

        public void BuildStreamRequest(string profileName, Guid? conversationId, string username, string message, int? maxMessages, string? database, string? ragTarget, int? maxRagDocs)
        {
            ProfileName = profileName;
            Completion = message;
            ConversationId = conversationId;
            MaxMessageHistory = maxMessages ?? 0;
            ProfileModifiers = new BaseCompletionDTO()
            {
                User = username ?? "Unknown",
                Stream = true
            };
            if (database is not null && ragTarget is not null)
            {
                RagData = new RagRequestData()
                {
                    RagDatabase = database,
                    RagTarget = ragTarget,
                    MaxRagDocs = maxRagDocs ?? 5
                };
            }
        }
    }
}
