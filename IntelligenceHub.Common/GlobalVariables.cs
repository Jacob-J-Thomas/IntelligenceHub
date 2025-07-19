namespace IntelligenceHub.Common
{
    /// <summary>
    /// Contains global variables and enumerations used throughout the IntelligenceHub application.
    /// </summary>
    public static class GlobalVariables
    {
        /// <summary>
        /// Specifies the format of the response.
        /// </summary>
        public enum ResponseFormat
        {
            Json,
            Text
        }

        /// <summary>
        /// Specifies the requirement for tool execution.
        /// </summary>
        public enum ToolExecutionRequirement
        {
            Required,
            Auto,
            None,
        }

        /// <summary>
        /// Specifies the reasons for finishing a process.
        /// </summary>
        public enum FinishReasons
        {
            Stop,
            Length,
            ToolCalls,
            ContentFilter,
            Error,
            TooManyRequests
        }

        /// <summary>
        /// Specifies the roles in the system.
        /// </summary>
        public enum Role
        {
            User,
            Assistant,
            System,
            Tool
        }

        /// <summary>
        /// Specifies the tools available in the system.
        /// </summary>
        public enum SystemTools
        {
            Image_Gen,
            Chat_Recursion,
        }

        /// <summary>
        /// Specifies the hosts for AGI services.
        /// </summary>
        public enum AGIServiceHost
        {
            Azure,
            OpenAI,
            Anthropic,
            None
        }

        /// <summary>
        /// Specifies the vector database provider.
        /// </summary>
        public enum RagServiceHost
        {
            Azure,
            Weaviate,
            None
        }

        /// <summary>
        /// Specifies the types of queries.
        /// </summary>
        public enum QueryType
        {
            Simple,
            Full,
            Semantic,
            Vector,
            VectorSemanticHybrid,
            VectorSimpleHybrid
        }

        /// <summary>
        /// Specifies the methods of search interpolation.
        /// </summary>
        public enum SearchInterpolation
        {
            Linear,
            Constant,
            Quadratic,
            Logarithmic
        }

        /// <summary>
        /// Specifies the methods of search aggregation.
        /// </summary>
        public enum SearchAggregation
        {
            Sum,
            Average,
            Minimum,
            Maximum,
            FirstMatching
        }

        /// <summary>
        /// Specifies the client policies.
        /// </summary>
        public enum ClientPolicies
        {
            AzureAIClientPolicy,
            OpenAIClientPolicy,
            AnthropicAIClientPolicy,
            ToolClientPolicy
        }

        /// <summary>
        /// Specifies the status codes for API responses.
        /// </summary>
        public enum APIResponseStatusCodes
        {
            Ok,
            BadRequest,
            NotFound,
            InternalError,
            TooManyRequests,
        }

        /// <summary>
        /// Specifies the access level of a user.
        /// </summary>
        public enum AccessLevel
        {
            Free,
            Paid,
        }

        /// <summary>
        /// The default model for Anthropic.
        /// </summary>
        public const string DefaultAnthropicModel = "claude-3-7-sonnet";

        /// <summary>
        /// The default model for OpenAI.
        /// </summary>
        public const string DefaultOpenAIModel = "gpt-4o";

        /// <summary>
        /// The valid models for Anthropic.
        /// </summary>
        public static readonly string[] ValidAnthropicModels =
        {
                "claude-3-7-sonnet",
                "claude-3-5-sonnet",
                "claude-3-5-haiku",
                "claude-3-opus",
                "claude-3-haiku"
            };

        /// <summary>
        /// The valid models and context limits for OpenAI.
        /// </summary>
        public static readonly Dictionary<string, int> ValidOpenAIModelsAndContextLimits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "o1", 4096 },
                { "o3-mini", 2048 },
                { "gpt-4.1", 8192 },
                { "gpt-4o", 8192 },
                { "gpt-4o-mini", 4096 },
                { "gpt-4", 8192 },
                { "gpt-3.5-turbo", 4096 }
            };

        /// <summary>
        /// The policy for elevated authentication.
        /// </summary>
        public const string ElevatedAuthPolicy = "AdminPolicy";

        /// <summary>
        /// The system message for RAG requests.
        /// </summary>
        public const string RagMetadataGenSystemMessage =
            "You are part of an API that chunks documents for retrieval augmented generation tasks. Your job " +
            "is to take the requests, which are sent to you programatically, and shorten the data into a topic, " +
            "keywords, or another form of data. Take care to only provide the data requested in the completion, " +
            "as any words unrelated to the completion request will be interpreted as part of the topic or " +
            "keyword. It is vital that you keep all responses brief as well, as any response from you that " +
            "exceeds 255 characters will be truncated.";

        /// <summary>
        /// The prepended instructions for RAG requests.
        /// </summary>
        public const string RagRequestPrependedInstructions =
            "Below you will find a set of documents, each delimited with tripple backticks. " +
            "Please use these documents to inform your response to the dialogue below the documents. " +
            "If you use one of the sources, please reference it in your response using markdown like so: [SourceName](SourceLink)." +
            "If no SourceLink is present, only provide the sourcename. If no documents were provided, simply respond to the user's" +
            "message like you normally would.";

        /// <summary>
        /// System message instructions for generating a RAG search intent.
        /// </summary>
        public const string RagIntentGenSystemMessage =
            "You are part of an API that generates search intents for retrieval augmented generation tasks. Your job is to look at all of the current context," +
            "and attempt to create a simple query consisting of a few key words, or a short search intent, similar to values that you would feed into a search" +
            "engine. Only provide the query itself with no additional data, as your output will be fed directly to an Azure AI search service query request, " +
            "and this will obfuscate the result.";

        /// <summary>
        /// The default vector scoring profile name.
        /// </summary>
        public const string DefaultVectorScoringProfile = "vector-search-profile";

        /// <summary>
        /// The default model for embeddings.
        /// </summary>
        public const string DefaultAzureSearchEmbeddingModel = "text-embedding-3-large";

        /// <summary>
        /// The default embedding model used when indexing with Weaviate.
        /// </summary>
        public const string DefaultWeaviateEmbeddingModel = "Snowflake/snowflake-arctic-embed-l-v2.0"; // currently this value is only used in Validation and saving database defaults. The Weaviate client defaults to this value automatically, and therefore doesn't require it

        /// <summary>
        /// The default model for image generation.
        /// </summary>
        public const string DefaultImageGenModel = "dall-e-3";

        /// <summary>
        /// The default exception message.
        /// </summary>
        public const string DefaultExceptionMessage = "Internal Server Error, please reattempt. If this issue persists please contact the system administrator.";

        // RAG Settings

        /// <summary>
        /// The default number of attachments for RAG.
        /// </summary>
        public const int DefaultRagAttachmentNumber = 3;

        /// <summary>
        /// The default chunk overlap for RAG.
        /// </summary>
        public const double DefaultChunkOverlap = .1;

        /// <summary>
        /// The maximum number of retries for AI search service.
        /// </summary>
        public const int AISearchServiceMaxRetries = 5;

        /// <summary>
        /// The initial delay for AI search service retries.
        /// </summary>
        public const int AISearchServiceInitialDelay = 2;

        /// <summary>
        /// The maximum delay for AI search service retries.
        /// </summary>
        public const int AISearchServiceMaxDelay = 20;

        /// <summary>
        /// The default freshness boost for scoring.
        /// </summary>
        public const int DefaultScoringFreshnessBoost = 1;

        /// <summary>
        /// The default duration in days for scoring boost.
        /// </summary>
        public const int DefaultScoringBoostDurationDays = 180;

        /// <summary>
        /// The default tag boost for scoring.
        /// </summary>
        public const int DefaultScoringTagBoost = 1;

        /// <summary>
        /// The maximum number of requests allowed per window for free users.
        /// </summary>
        public const int FreeUserRateLimitRequests = 10;

        /// <summary>
        /// The window size in seconds for free user rate limiting.
        /// </summary>
        public const int FreeUserRateLimitWindowSeconds = 60;

        /// <summary>
        /// The maximum number of requests allowed per window for paid users.
        /// </summary>
        public const int PaidUserRateLimitRequests = 60;

        /// <summary>
        /// The window size in seconds for paid user rate limiting.
        /// </summary>
        public const int PaidUserRateLimitWindowSeconds = 60;

        /// <summary>
        /// The monthly request limit for free tier users.
        /// </summary>
        public const int FreeTierMonthlyLimit = 100;
    }
}
