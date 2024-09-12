using Newtonsoft.Json;
using System.ComponentModel;

namespace IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs
{
    public class RagRequestData
    {
        public string RagDatabase { get; set; }
        [DefaultValue(5)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int? MaxRagDocs { get; set; }
        [DefaultValue("Content")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string RagTarget { get; set; }
    }
}
