namespace IntelligenceHub.Tests.Common.DTOs
{
    public class GeneratedDTOs
    {

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class CompletionRequest
        {
            [Newtonsoft.Json.JsonProperty("conversationId", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public Guid? ConversationId { get; set; }

            [Newtonsoft.Json.JsonProperty("profileOptions", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public Profile ProfileOptions { get; set; }

            [Newtonsoft.Json.JsonProperty("messages", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<Message> Messages { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class CompletionResponse
        {
            [Newtonsoft.Json.JsonProperty("messages", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<Message> Messages { get; set; }

            [Newtonsoft.Json.JsonProperty("toolCalls", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public IDictionary<string, string> ToolCalls { get; set; }

            [Newtonsoft.Json.JsonProperty("toolExecutionResponses", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<HttpResponseMessage> ToolExecutionResponses { get; set; }

            [Newtonsoft.Json.JsonProperty("finishReason", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
            public FinishReason FinishReason { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class CompletionStreamChunk
        {
            [Newtonsoft.Json.JsonProperty("completionUpdate", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string CompletionUpdate { get; set; }

            [Newtonsoft.Json.JsonProperty("base64Image", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Base64Image { get; set; }

            [Newtonsoft.Json.JsonProperty("role", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
            public Role Role { get; set; }

            [Newtonsoft.Json.JsonProperty("finishReason", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
            public FinishReason FinishReason { get; set; }

            [Newtonsoft.Json.JsonProperty("toolCalls", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public IDictionary<string, string> ToolCalls { get; set; }

            [Newtonsoft.Json.JsonProperty("toolExecutionResponses", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<HttpResponseMessage> ToolExecutionResponses { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public enum FinishReason
        {

            [System.Runtime.Serialization.EnumMember(Value = @"Stop")]
            Stop = 0,

            [System.Runtime.Serialization.EnumMember(Value = @"Length")]
            Length = 1,

            [System.Runtime.Serialization.EnumMember(Value = @"ToolCalls")]
            ToolCalls = 2,

            [System.Runtime.Serialization.EnumMember(Value = @"ContentFilter")]
            ContentFilter = 3,

            [System.Runtime.Serialization.EnumMember(Value = @"Error")]
            Error = 4,

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class Function
        {
            [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Name { get; set; }

            [Newtonsoft.Json.JsonProperty("description", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Description { get; set; }

            [Newtonsoft.Json.JsonProperty("parameters", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public Parameters Parameters { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class HttpContent
        {
            [Newtonsoft.Json.JsonProperty("headers", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<StringStringIEnumerableKeyValuePair> Headers { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class HttpMethod
        {
            [Newtonsoft.Json.JsonProperty("method", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Method { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class HttpRequestMessage
        {
            [Newtonsoft.Json.JsonProperty("version", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Version { get; set; }

            [Newtonsoft.Json.JsonProperty("versionPolicy", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
            public HttpVersionPolicy VersionPolicy { get; set; }

            [Newtonsoft.Json.JsonProperty("content", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public HttpContent Content { get; set; }

            [Newtonsoft.Json.JsonProperty("method", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public HttpMethod Method { get; set; }

            [Newtonsoft.Json.JsonProperty("requestUri", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public Uri RequestUri { get; set; }

            [Newtonsoft.Json.JsonProperty("headers", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<StringStringIEnumerableKeyValuePair> Headers { get; set; }

            [Newtonsoft.Json.JsonProperty("properties", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            [Obsolete]
            public IDictionary<string, object> Properties { get; set; }

            [Newtonsoft.Json.JsonProperty("options", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public IDictionary<string, object> Options { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class HttpResponseMessage
        {
            [Newtonsoft.Json.JsonProperty("version", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Version { get; set; }

            [Newtonsoft.Json.JsonProperty("content", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public HttpContent Content { get; set; }

            [Newtonsoft.Json.JsonProperty("statusCode", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
            public HttpStatusCode StatusCode { get; set; }

            [Newtonsoft.Json.JsonProperty("reasonPhrase", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string ReasonPhrase { get; set; }

            [Newtonsoft.Json.JsonProperty("headers", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<StringStringIEnumerableKeyValuePair> Headers { get; set; }

            [Newtonsoft.Json.JsonProperty("trailingHeaders", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<StringStringIEnumerableKeyValuePair> TrailingHeaders { get; set; }

            [Newtonsoft.Json.JsonProperty("requestMessage", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public HttpRequestMessage RequestMessage { get; set; }

            [Newtonsoft.Json.JsonProperty("isSuccessStatusCode", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public bool IsSuccessStatusCode { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public enum HttpStatusCode
        {

            [System.Runtime.Serialization.EnumMember(Value = @"Continue")]
            Continue = 0,

            [System.Runtime.Serialization.EnumMember(Value = @"SwitchingProtocols")]
            SwitchingProtocols = 1,

            [System.Runtime.Serialization.EnumMember(Value = @"Processing")]
            Processing = 2,

            [System.Runtime.Serialization.EnumMember(Value = @"EarlyHints")]
            EarlyHints = 3,

            [System.Runtime.Serialization.EnumMember(Value = @"OK")]
            OK = 4,

            [System.Runtime.Serialization.EnumMember(Value = @"Created")]
            Created = 5,

            [System.Runtime.Serialization.EnumMember(Value = @"Accepted")]
            Accepted = 6,

            [System.Runtime.Serialization.EnumMember(Value = @"NonAuthoritativeInformation")]
            NonAuthoritativeInformation = 7,

            [System.Runtime.Serialization.EnumMember(Value = @"NoContent")]
            NoContent = 8,

            [System.Runtime.Serialization.EnumMember(Value = @"ResetContent")]
            ResetContent = 9,

            [System.Runtime.Serialization.EnumMember(Value = @"PartialContent")]
            PartialContent = 10,

            [System.Runtime.Serialization.EnumMember(Value = @"MultiStatus")]
            MultiStatus = 11,

            [System.Runtime.Serialization.EnumMember(Value = @"AlreadyReported")]
            AlreadyReported = 12,

            [System.Runtime.Serialization.EnumMember(Value = @"IMUsed")]
            IMUsed = 13,

            [System.Runtime.Serialization.EnumMember(Value = @"MultipleChoices")]
            MultipleChoices = 14,

            [System.Runtime.Serialization.EnumMember(Value = @"MovedPermanently")]
            MovedPermanently = 15,

            [System.Runtime.Serialization.EnumMember(Value = @"Found")]
            Found = 16,

            [System.Runtime.Serialization.EnumMember(Value = @"SeeOther")]
            SeeOther = 17,

            [System.Runtime.Serialization.EnumMember(Value = @"NotModified")]
            NotModified = 18,

            [System.Runtime.Serialization.EnumMember(Value = @"UseProxy")]
            UseProxy = 19,

            [System.Runtime.Serialization.EnumMember(Value = @"Unused")]
            Unused = 20,

            [System.Runtime.Serialization.EnumMember(Value = @"TemporaryRedirect")]
            TemporaryRedirect = 21,

            [System.Runtime.Serialization.EnumMember(Value = @"PermanentRedirect")]
            PermanentRedirect = 22,

            [System.Runtime.Serialization.EnumMember(Value = @"BadRequest")]
            BadRequest = 23,

            [System.Runtime.Serialization.EnumMember(Value = @"Unauthorized")]
            Unauthorized = 24,

            [System.Runtime.Serialization.EnumMember(Value = @"PaymentRequired")]
            PaymentRequired = 25,

            [System.Runtime.Serialization.EnumMember(Value = @"Forbidden")]
            Forbidden = 26,

            [System.Runtime.Serialization.EnumMember(Value = @"NotFound")]
            NotFound = 27,

            [System.Runtime.Serialization.EnumMember(Value = @"MethodNotAllowed")]
            MethodNotAllowed = 28,

            [System.Runtime.Serialization.EnumMember(Value = @"NotAcceptable")]
            NotAcceptable = 29,

            [System.Runtime.Serialization.EnumMember(Value = @"ProxyAuthenticationRequired")]
            ProxyAuthenticationRequired = 30,

            [System.Runtime.Serialization.EnumMember(Value = @"RequestTimeout")]
            RequestTimeout = 31,

            [System.Runtime.Serialization.EnumMember(Value = @"Conflict")]
            Conflict = 32,

            [System.Runtime.Serialization.EnumMember(Value = @"Gone")]
            Gone = 33,

            [System.Runtime.Serialization.EnumMember(Value = @"LengthRequired")]
            LengthRequired = 34,

            [System.Runtime.Serialization.EnumMember(Value = @"PreconditionFailed")]
            PreconditionFailed = 35,

            [System.Runtime.Serialization.EnumMember(Value = @"RequestEntityTooLarge")]
            RequestEntityTooLarge = 36,

            [System.Runtime.Serialization.EnumMember(Value = @"RequestUriTooLong")]
            RequestUriTooLong = 37,

            [System.Runtime.Serialization.EnumMember(Value = @"UnsupportedMediaType")]
            UnsupportedMediaType = 38,

            [System.Runtime.Serialization.EnumMember(Value = @"RequestedRangeNotSatisfiable")]
            RequestedRangeNotSatisfiable = 39,

            [System.Runtime.Serialization.EnumMember(Value = @"ExpectationFailed")]
            ExpectationFailed = 40,

            [System.Runtime.Serialization.EnumMember(Value = @"MisdirectedRequest")]
            MisdirectedRequest = 41,

            [System.Runtime.Serialization.EnumMember(Value = @"UnprocessableEntity")]
            UnprocessableEntity = 42,

            [System.Runtime.Serialization.EnumMember(Value = @"Locked")]
            Locked = 43,

            [System.Runtime.Serialization.EnumMember(Value = @"FailedDependency")]
            FailedDependency = 44,

            [System.Runtime.Serialization.EnumMember(Value = @"UpgradeRequired")]
            UpgradeRequired = 45,

            [System.Runtime.Serialization.EnumMember(Value = @"PreconditionRequired")]
            PreconditionRequired = 46,

            [System.Runtime.Serialization.EnumMember(Value = @"TooManyRequests")]
            TooManyRequests = 47,

            [System.Runtime.Serialization.EnumMember(Value = @"RequestHeaderFieldsTooLarge")]
            RequestHeaderFieldsTooLarge = 48,

            [System.Runtime.Serialization.EnumMember(Value = @"UnavailableForLegalReasons")]
            UnavailableForLegalReasons = 49,

            [System.Runtime.Serialization.EnumMember(Value = @"InternalServerError")]
            InternalServerError = 50,

            [System.Runtime.Serialization.EnumMember(Value = @"NotImplemented")]
            NotImplemented = 51,

            [System.Runtime.Serialization.EnumMember(Value = @"BadGateway")]
            BadGateway = 52,

            [System.Runtime.Serialization.EnumMember(Value = @"ServiceUnavailable")]
            ServiceUnavailable = 53,

            [System.Runtime.Serialization.EnumMember(Value = @"GatewayTimeout")]
            GatewayTimeout = 54,

            [System.Runtime.Serialization.EnumMember(Value = @"HttpVersionNotSupported")]
            HttpVersionNotSupported = 55,

            [System.Runtime.Serialization.EnumMember(Value = @"VariantAlsoNegotiates")]
            VariantAlsoNegotiates = 56,

            [System.Runtime.Serialization.EnumMember(Value = @"InsufficientStorage")]
            InsufficientStorage = 57,

            [System.Runtime.Serialization.EnumMember(Value = @"LoopDetected")]
            LoopDetected = 58,

            [System.Runtime.Serialization.EnumMember(Value = @"NotExtended")]
            NotExtended = 59,

            [System.Runtime.Serialization.EnumMember(Value = @"NetworkAuthenticationRequired")]
            NetworkAuthenticationRequired = 60,

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public enum HttpVersionPolicy
        {

            [System.Runtime.Serialization.EnumMember(Value = @"RequestVersionOrLower")]
            RequestVersionOrLower = 0,

            [System.Runtime.Serialization.EnumMember(Value = @"RequestVersionOrHigher")]
            RequestVersionOrHigher = 1,

            [System.Runtime.Serialization.EnumMember(Value = @"RequestVersionExact")]
            RequestVersionExact = 2,

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class IndexDocument
        {
            [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public int Id { get; set; }

            [Newtonsoft.Json.JsonProperty("title", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Title { get; set; }

            [Newtonsoft.Json.JsonProperty("content", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Content { get; set; }

            [Newtonsoft.Json.JsonProperty("topic", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Topic { get; set; }

            [Newtonsoft.Json.JsonProperty("keywords", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Keywords { get; set; }

            [Newtonsoft.Json.JsonProperty("source", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Source { get; set; }

            [Newtonsoft.Json.JsonProperty("created", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public DateTimeOffset Created { get; set; }

            [Newtonsoft.Json.JsonProperty("modified", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public DateTimeOffset Modified { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class IndexMetadata
        {
            [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Name { get; set; }

            [Newtonsoft.Json.JsonProperty("queryType", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string QueryType { get; set; }

            [Newtonsoft.Json.JsonProperty("indexingInterval", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string IndexingInterval { get; set; }

            [Newtonsoft.Json.JsonProperty("embeddingModel", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string EmbeddingModel { get; set; }

            [Newtonsoft.Json.JsonProperty("maxRagAttachments", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public int MaxRagAttachments { get; set; }

            [Newtonsoft.Json.JsonProperty("chunkOverlap", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public float ChunkOverlap { get; set; }

            [Newtonsoft.Json.JsonProperty("generateTopic", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public bool GenerateTopic { get; set; }

            [Newtonsoft.Json.JsonProperty("generateKeywords", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public bool GenerateKeywords { get; set; }

            [Newtonsoft.Json.JsonProperty("generateTitleVector", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public bool GenerateTitleVector { get; set; }

            [Newtonsoft.Json.JsonProperty("generateContentVector", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public bool GenerateContentVector { get; set; }

            [Newtonsoft.Json.JsonProperty("generateTopicVector", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public bool GenerateTopicVector { get; set; }

            [Newtonsoft.Json.JsonProperty("generateKeywordVector", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public bool GenerateKeywordVector { get; set; }

            [Newtonsoft.Json.JsonProperty("scoringProfile", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public IndexScoringProfile ScoringProfile { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class IndexScoringProfile
        {
            [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Name { get; set; }

            [Newtonsoft.Json.JsonProperty("aggregation", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Aggregation { get; set; }

            [Newtonsoft.Json.JsonProperty("interpolation", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Interpolation { get; set; }

            [Newtonsoft.Json.JsonProperty("freshnessBoost", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public double FreshnessBoost { get; set; }

            [Newtonsoft.Json.JsonProperty("boostDurationDays", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public int BoostDurationDays { get; set; }

            [Newtonsoft.Json.JsonProperty("tagBoost", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public double TagBoost { get; set; }

            [Newtonsoft.Json.JsonProperty("weights", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public IDictionary<string, double> Weights { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class Message
        {
            [Newtonsoft.Json.JsonProperty("role", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
            public Role Role { get; set; }

            [Newtonsoft.Json.JsonProperty("content", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Content { get; set; }

            [Newtonsoft.Json.JsonProperty("base64Image", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Base64Image { get; set; }

            [Newtonsoft.Json.JsonProperty("timeStamp", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public DateTimeOffset TimeStamp { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class Parameters
        {
            [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Type { get; set; }

            [Newtonsoft.Json.JsonProperty("properties", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public IDictionary<string, Property> Properties { get; set; }

            [Newtonsoft.Json.JsonProperty("required", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<string> Required { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class ProblemDetails
        {
            [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Type { get; set; }

            [Newtonsoft.Json.JsonProperty("title", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Title { get; set; }

            [Newtonsoft.Json.JsonProperty("status", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public int? Status { get; set; }

            [Newtonsoft.Json.JsonProperty("detail", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Detail { get; set; }

            [Newtonsoft.Json.JsonProperty("instance", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Instance { get; set; }

            private IDictionary<string, object> _additionalProperties;

            [Newtonsoft.Json.JsonExtensionData]
            public IDictionary<string, object> AdditionalProperties
            {
                get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
                set { _additionalProperties = value; }
            }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class Profile
        {
            [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public int Id { get; set; }

            [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Name { get; set; }

            [Newtonsoft.Json.JsonProperty("model", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Model { get; set; }

            [Newtonsoft.Json.JsonProperty("ragDatabase", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string RagDatabase { get; set; }

            [Newtonsoft.Json.JsonProperty("frequency_Penalty", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public float? Frequency_Penalty { get; set; }

            [Newtonsoft.Json.JsonProperty("presence_Penalty", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public float? Presence_Penalty { get; set; }

            [Newtonsoft.Json.JsonProperty("temperature", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public float? Temperature { get; set; }

            [Newtonsoft.Json.JsonProperty("top_P", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public float? Top_P { get; set; }

            [Newtonsoft.Json.JsonProperty("max_Tokens", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public int? Max_Tokens { get; set; }

            [Newtonsoft.Json.JsonProperty("top_Logprobs", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public int? Top_Logprobs { get; set; }

            [Newtonsoft.Json.JsonProperty("logprobs", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public bool? Logprobs { get; set; }

            [Newtonsoft.Json.JsonProperty("user", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string User { get; set; }

            [Newtonsoft.Json.JsonProperty("tool_Choice", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Tool_Choice { get; set; }

            [Newtonsoft.Json.JsonProperty("response_Format", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Response_Format { get; set; }

            [Newtonsoft.Json.JsonProperty("system_Message", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string System_Message { get; set; }

            [Newtonsoft.Json.JsonProperty("stop", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<string> Stop { get; set; }

            [Newtonsoft.Json.JsonProperty("tools", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<Tool> Tools { get; set; }

            [Newtonsoft.Json.JsonProperty("maxMessageHistory", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public int? MaxMessageHistory { get; set; }

            [Newtonsoft.Json.JsonProperty("return_Recursion", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public bool? Return_Recursion { get; set; }

            [Newtonsoft.Json.JsonProperty("reference_Profiles", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<string> Reference_Profiles { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class Property
        {
            [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public int? Id { get; set; }

            [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Type { get; set; }

            [Newtonsoft.Json.JsonProperty("description", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Description { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class RagUpsertRequest
        {
            [Newtonsoft.Json.JsonProperty("documents", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<IndexDocument> Documents { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public enum Role
        {

            [System.Runtime.Serialization.EnumMember(Value = @"User")]
            User = 0,

            [System.Runtime.Serialization.EnumMember(Value = @"Assistant")]
            Assistant = 1,

            [System.Runtime.Serialization.EnumMember(Value = @"System")]
            System = 2,

            [System.Runtime.Serialization.EnumMember(Value = @"Tool")]
            Tool = 3,

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class StringStringIEnumerableKeyValuePair
        {
            [Newtonsoft.Json.JsonProperty("key", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Key { get; set; }

            [Newtonsoft.Json.JsonProperty("value", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public ICollection<string> Value { get; set; }

        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
        public partial class Tool
        {
            [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string Type { get; set; }

            [Newtonsoft.Json.JsonProperty("function", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public Function Function { get; set; }

            [Newtonsoft.Json.JsonProperty("executionUrl", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string ExecutionUrl { get; set; }

            [Newtonsoft.Json.JsonProperty("executionMethod", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string ExecutionMethod { get; set; }

            [Newtonsoft.Json.JsonProperty("executionBase64Key", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public string ExecutionBase64Key { get; set; }

        }
    }
}
