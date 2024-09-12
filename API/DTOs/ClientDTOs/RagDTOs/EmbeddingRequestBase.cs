namespace IntelligenceHub.API.DTOs.ClientDTOs.EmbeddingDTOs
{
    public class EmbeddingRequestBase
    {
        public string Input { get; set; }
        public string Model { get; set; } = "text-embedding-3-small";
        public string Encoding_Format { get; set; } = "float";
        public int Dimensions { get; set; } = 1536;
        public string User { get; set; } = "user"; // as far as I can tell this shouldn't be required,
                                                   // but openAI is complaining if User is null, or empty
                                                   // the property is nullable, and other types work fine
                                                   // when utilized this way. But if this continues to be
                                                   // a problem report it as a bug
    }
}
