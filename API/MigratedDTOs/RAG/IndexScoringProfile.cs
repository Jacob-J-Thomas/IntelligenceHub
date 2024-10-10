namespace OpenAICustomFunctionCallingAPI.API.MigratedDTOs.RAG
{
    public class IndexScoringProfile
    {
        public string Name { get; set; }
        public string Aggregation {  get; set; }
        public string Interpolation { get; set; }
        public double FreshnessBoost { get; set; }
        public int BoostDurationDays { get; set; }
        public double TagBoost { get; set; }
        public Dictionary<string, double> Weights { get; set; } = new Dictionary<string, double>();
    }
}
