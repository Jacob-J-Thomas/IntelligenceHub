using IntelligenceHub.Common.Attributes;

namespace IntelligenceHub.DAL.Models
{
    // map both the ScoringProfile and the IndexMetadata here
    [TableName("IndexMetadata")]
    public class DbIndex
    {
        public string Name { get; set; } = string.Empty;
        public string DefaultScoringProfile { get; set; } = string.Empty;
    }
}
