using Nest;
using OpenAICustomFunctionCallingAPI.Common.Attributes;

namespace OpenAICustomFunctionCallingAPI.API.DTOs.DataAccessDTOs
{
    [TableName("RagIndexMetaData")]
    public class RagIndexMetaDataDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string VectorModel { get; set; }
        public string EncodingFormat { get; set; }
        public int ChunkLength { get; set; }
        public float ChunkOverlap { get; set; }
        public int Dimensions { get; set; }
        public bool GenerateTopic { get; set; }
        public bool GenerateKeywords { get; set; }
        public bool GenerateTitleVector { get; set; }
        public bool GenerateContentVector { get; set; }
        public bool GenerateTopicVector { get; set; }
        public bool GenerateKeywordVector { get; set; }
        public bool TrackDocumentAccessCount { get; set; }
    }
}
