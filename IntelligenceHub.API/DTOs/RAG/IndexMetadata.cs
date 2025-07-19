using static IntelligenceHub.Common.GlobalVariables;
using System.Text.Json.Serialization;

namespace IntelligenceHub.API.DTOs.RAG
{
    /// <summary>
    /// Represents metadata used to configure a RAG index.
    /// </summary>
    public class IndexMetadata
    {
        /// <summary>
        /// Gets or sets the name of the index.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the query type used when searching the index.
        /// </summary>
        public QueryType QueryType { get; set; }

        /// <summary>
        /// Gets or sets the host used for generating vector embeddings.
        /// </summary>
        [JsonIgnore]
        public AGIServiceHost? GenerationHost { get; set; }

        /// <summary>
        /// Gets or sets the service that stores the vectors.
        /// </summary>
        [JsonIgnore]
        public RagServiceHost? RagHost { get; set; }

        /// <summary>
        /// Gets or sets how frequently the index should be rebuilt.
        /// </summary>
        [JsonIgnore]
        public TimeSpan? IndexingInterval { get; set; }

        /// <summary>
        /// Gets or sets the embedding model used for vectorization.
        /// </summary>
        [JsonIgnore]
        public string? EmbeddingModel { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of attachments to return in RAG queries.
        /// </summary>
        [JsonIgnore]
        public int? MaxRagAttachments { get; set; }

        /// <summary>
        /// Gets or sets the overlap between adjacent chunks of text.
        /// </summary>
        [JsonIgnore]
        public double? ChunkOverlap { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the topic should be generated.
        /// </summary>
        [JsonIgnore]
        public bool? GenerateTopic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether keywords should be generated.
        /// </summary>
        [JsonIgnore]
        public bool? GenerateKeywords { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a title vector should be generated.
        /// </summary>
        public bool? GenerateTitleVector { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a content vector should be generated.
        /// </summary>
        public bool? GenerateContentVector { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a topic vector should be generated.
        /// </summary>
        public bool? GenerateTopicVector { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a keyword vector should be generated.
        /// </summary>
        public bool? GenerateKeywordVector { get; set; }

        /// <summary>
        /// Gets or sets the scoring profile applied when ranking results.
        /// </summary>
        [JsonIgnore]
        public IndexScoringProfile? ScoringProfile { get; set; } = new IndexScoringProfile();
    }
}
