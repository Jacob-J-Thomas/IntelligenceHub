using OpenAI.Chat;
using IntelligenceHub.API;

namespace IntelligenceHub.Common
{
    public static class GlobalVariables
    {
        public static string RagRequestSystemMessage { get; set; } = 
            "You are part of an API that chunks documents for retrieval augmented " +
            "generation tasks. Your job is to take the requests, which are sent to you programatically, " +
            "and shorten the data into a topic, keywords, or another form of data. Please take care to " +
            "only provide the data requested in the completion, as any words unrelated to the completion " +
            "request will be interpreted as part of the topic or keyword.";

        public static string RagRequestPrependedInstructions { get; set; } = 
            "Below you will find a set of documents, each delimited with tripple backticks. " +
                "Please use these documents to inform your response to the dialogue below the documents. " +
                "If you use one of the sources, please reference it in your response using markdown like so: [SourceName](SourceLink)." +
                "If no SourceLink is present, only provide the sourcename.\n\n";

        //public static readonly Dictionary<ResponseFormat, string> ResponseFormats = new Dictionary<ResponseFormat, string>
        //{
        //    { ResponseFormat.Json, "json" },
        //    { ResponseFormat.String, "string" }
        //};

        public enum ResponseFormat
        {
            Json,
            Text
        }

        public enum ToolExecutionRequirement
        {
            Required,
            Auto,
            None,
        }
    }
}
