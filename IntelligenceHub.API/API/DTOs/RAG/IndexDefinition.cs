using Azure.Search.Documents.Indexes;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs.RAG
{
    public class IndexDefinition
    {
        [SimpleField(IsKey = true)]
        public string id { get; set; }
        [SimpleField(IsFilterable = true)]
        public string parentId { get; set; }
        [SearchableField(IsFilterable = true)]
        public string title { get; set; }
        [SearchableField(IsFilterable = true)]
        public string content { get; set; }
        [SearchableField(IsFilterable = true)]
        public string topic { get; set; }
        [SearchableField(IsFilterable = true)]
        public string keywords { get; set; }
        [SimpleField]
        public string source { get; set; }
        [SimpleField(IsFilterable = true)]
        public DateTimeOffset created { get; set; }
        [SimpleField(IsFilterable = true)]
        public DateTimeOffset modified { get; set; }
        [SimpleField]
        public int chunkSplits { get; set; }
        [VectorSearchField(VectorSearchDimensions = 3072, VectorSearchProfileName = DefaultVectorSearchProfile, IsHidden = false)]
        public IReadOnlyList<float> titleVector { get; set; }
        [VectorSearchField(VectorSearchDimensions = 3072, VectorSearchProfileName = DefaultVectorSearchProfile, IsHidden = false)]
        public IReadOnlyList<float> contentVector { get; set; }
        [VectorSearchField(VectorSearchDimensions = 3072, VectorSearchProfileName = DefaultVectorSearchProfile, IsHidden = false)]
        public IReadOnlyList<float> topicVector { get; set; }
        [VectorSearchField(VectorSearchDimensions = 3072, VectorSearchProfileName = DefaultVectorSearchProfile, IsHidden = false)]
        public IReadOnlyList<float> keywordVector { get; set; }
    }
}
