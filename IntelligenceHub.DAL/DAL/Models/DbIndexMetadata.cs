using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    // map both the ScoringProfile and the IndexMetadata here
    [Table("IndexMetadata")]
    public class DbIndexMetadata
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? QueryType { get; set; }
        [Required]
        public long? IndexingInterval { get; set; }
        public string? EmbeddingModel { get; set; }
        public int MaxRagAttachments { get; set; } = 3;
        public double ChunkOverlap { get; set; } = 0.1;
        public bool GenerateTopic { get; set; }
        public bool GenerateKeywords { get; set; }
        public bool GenerateTitleVector { get; set; }
        public bool GenerateContentVector { get; set; }
        public bool GenerateTopicVector { get; set; }
        public bool GenerateKeywordVector { get; set; }
        public string? DefaultScoringProfile { get; set; }
        public string? ScoringAggregation { get; set; }
        public string? ScoringInterpolation { get; set; }
        public double ScoringFreshnessBoost { get; set; } = 0.0;
        public int ScoringBoostDurationDays { get; set; } = 0;
        public double ScoringTagBoost { get; set; } = 0.0;
        public string? ScoringWeights { get; set; }
    }
}
