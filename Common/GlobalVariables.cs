using OpenAI.Chat;
using IntelligenceHub.API;
using IntelligenceHub.Common.Exceptions;

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

        public enum FinishReason
        {
            Stop,
            Length,
            ToolCalls,
            ContentFilter,
        }

        public enum Role
        {
            User,
            Assistant,
            System,
            Tool
        }

        // Move these to an extension method class for the above
        public static FinishReason ConvertStringToFinishReason(string finishReason)
        {
            if (finishReason == ChatFinishReason.Stop.ToString()) return FinishReason.Stop;
            if (finishReason == ChatFinishReason.Length.ToString()) return FinishReason.Length;
            if (finishReason == ChatFinishReason.ToolCalls.ToString()) return FinishReason.ToolCalls;
            if (finishReason == ChatFinishReason.FunctionCall.ToString()) return FinishReason.ToolCalls;
            if (finishReason == ChatFinishReason.ContentFilter.ToString()) return FinishReason.ContentFilter;

            throw new IntelligenceHubException(500, "Could not convert chat finish reason to system finish reason");
        }

        public static Role ConvertStringToRole(string role)
        {
            if (role == ChatMessageRole.Assistant.ToString()) return Role.Assistant;
            if (role == ChatMessageRole.User.ToString()) return Role.User;
            if (role == ChatMessageRole.Tool.ToString()) return Role.Tool;
            if (role == ChatMessageRole.Function.ToString()) return Role.Tool;
            if (role == ChatMessageRole.System.ToString()) return Role.System;

            throw new IntelligenceHubException(500, "Could not convert chat role to system role");
        }
    }
}
