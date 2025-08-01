﻿using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Handlers;
using Microsoft.Extensions.Options;
using Moq;
using static IntelligenceHub.Common.GlobalVariables;
using IntelligenceHub.Common.Config;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class ValidationHandlerTests
    {
        private readonly ValidationHandler _handler;
        private readonly Mock<IOptionsMonitor<Settings>> _mockSettings;

        public ValidationHandlerTests()
        {
            // Setup dummy settings with valid models for Azure
            var settings = new Settings
            {
                // Assumed valid AGI models (in lowercase) for testing purposes.
                ValidAGIModels = new string[] { "gpt-4o", "gpt-3.5" }
            };

            _mockSettings = new Mock<IOptionsMonitor<Settings>>();
            _mockSettings.Setup(x => x.CurrentValue).Returns(settings);

            _handler = new ValidationHandler(_mockSettings.Object);
        }

        #region ValidateChatRequest Tests

        [Fact]
        public void ValidateChatRequest_WithValidRequest_ReturnsNull()
        {
            // Arrange
            var profile = new Profile
            {
                Name = "TestProfile",
                Model = "gpt-4o",
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text",
                ReferenceProfiles = new string[] { "ref1" },
                Tools = new List<API.DTOs.Tools.Tool>()
            };

            // Note: Based on ValidateMessageList, we assume that a valid request
            // should have at least one message that is not a Role.User (as per your sample logic).
            var message = new Message
            {
                Role = Role.User,
                Content = "Test content",
                User = "Tester"
            };

            var chatRequest = new CompletionRequest
            {
                ProfileOptions = profile,
                Messages = new List<Message> { message }
            };

            // Act
            var result = _handler.ValidateChatRequest(chatRequest);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateChatRequest_MissingProfileName_ReturnsError()
        {
            // Arrange: Profile name is missing (null)
            var profile = new Profile
            {
                Name = null,
                Model = "gpt-4o",
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            var message = new Message
            {
                Role = Role.Assistant,
                Content = "Test content",
                User = "Tester"
            };

            var chatRequest = new CompletionRequest
            {
                ProfileOptions = profile,
                Messages = new List<Message> { message }
            };

            // Act
            var result = _handler.ValidateChatRequest(chatRequest);

            // Assert
            Assert.Equal("A profile name must be included in the request body or route.", result);
        }

        [Fact]
        public void ValidateChatRequest_WithEmptyMessages_ReturnsError()
        {
            // Arrange: Create a valid profile but empty messages list
            var profile = new Profile
            {
                Name = "TestProfile",
                Model = "gpt-4o",
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            var chatRequest = new CompletionRequest
            {
                ProfileOptions = profile,
                Messages = new List<Message>() // empty list
            };

            // Act
            var result = _handler.ValidateChatRequest(chatRequest);

            // Assert
            Assert.Equal("The messages array was null or empty.", result);
        }

        [Fact]
        public void ValidateChatRequest_NoUserMessage_ReturnsError()
        {
            var profile = new Profile
            {
                Name = "TestProfile",
                Model = "gpt-4o",
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            var messages = new List<Message>
            {
                new Message { Role = Role.Assistant, Content = "Test", User = "Tester" }
            };

            var chatRequest = new CompletionRequest
            {
                ProfileOptions = profile,
                Messages = messages
            };

            var result = _handler.ValidateChatRequest(chatRequest);

            Assert.Equal("The messages array must contain at least one user message, but contains none.", result);
        }

        [Fact]
        public void ValidateChatRequest_InvalidModel_ReturnsError()
        {
            var profile = new Profile
            {
                Name = "TestProfile",
                Model = "bad-model",
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            var messages = new List<Message>
            {
                new Message { Role = Role.User, Content = "Test", User = "Tester" }
            };

            var chatRequest = new CompletionRequest
            {
                ProfileOptions = profile,
                Messages = messages
            };

            var expected = "The provided model name is not supported by Azure. Supported model names include: gpt-4o,gpt-3.5.";
            var result = _handler.ValidateChatRequest(chatRequest);

            Assert.Equal(expected, result);
        }

        #endregion

        #region ValidateAPIProfile Tests

        [Fact]
        public void ValidateAPIProfile_WithValidProfile_ReturnsNull()
        {
            // Arrange
            var profile = new Profile
            {
                Name = "ValidProfile",
                Model = "gpt-4o",
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            // Act
            var result = _handler.ValidateAPIProfile(profile);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateAPIProfile_WithNameAll_ReturnsError()
        {
            // Arrange
            var profile = new Profile
            {
                Name = "all",
                Model = "gpt-4o",
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            // Act
            var result = _handler.ValidateAPIProfile(profile);

            // Assert
            Assert.Equal("Profile name 'all' conflicts with the profile/get/all route.", result);
        }

        [Fact]
        public void ValidateAPIProfile_MissingName_ReturnsError()
        {
            var profile = new Profile
            {
                Name = null,
                Model = "gpt-4o",
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            var result = _handler.ValidateAPIProfile(profile);

            Assert.Equal("The 'Name' field is required.", result);
        }

        #endregion

        #region ValidateProfileOptions Tests

        [Fact]
        public void ValidateProfileOptions_WithValidOptions_ReturnsNull()
        {
            // Arrange
            var profile = new Profile
            {
                Name = "Profile1",
                Model = "gpt-4o",
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 30,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            // Act
            var result = _handler.ValidateProfileOptions(profile);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateProfileOptions_MissingModel_ReturnsError()
        {
            // Arrange: Missing model field
            var profile = new Profile
            {
                Name = "Profile1",
                Model = null,
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 30,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            // Act
            var result = _handler.ValidateProfileOptions(profile);

            // Assert
            Assert.Equal("The model parameter is required.", result);
        }

        [Fact]
        public void ValidateProfileOptions_MissingHost_ReturnsError()
        {
            var profile = new Profile
            {
                Name = "Profile1",
                Model = "gpt-4o",
                Host = AGIServiceHost.None,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 30,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            var result = _handler.ValidateProfileOptions(profile);

            Assert.Equal("The host parameter is required.", result);
        }

        [Fact]
        public void ValidateProfileOptions_FrequencyPenaltyOutOfRange_ReturnsError()
        {
            var profile = new Profile
            {
                Name = "Profile1",
                Model = "gpt-4o",
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 3,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 30,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            var result = _handler.ValidateProfileOptions(profile);

            Assert.Equal("FrequencyPenalty must be a value between -2 and 2.", result);
        }

        [Fact]
        public void ValidateProfileOptions_InvalidResponseFormat_ReturnsError()
        {
            var profile = new Profile
            {
                Name = "Profile1",
                Model = "gpt-4o",
                Host = AGIServiceHost.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 30,
                TopLogprobs = 0,
                ResponseFormat = "xml"
            };

            var result = _handler.ValidateProfileOptions(profile);

            Assert.Equal($"If ResponseType is set, it must either be equal to '{ResponseFormat.Text}' or '{ResponseFormat.Json}'.", result);
        }

        #endregion

        #region ValidateTool Tests

        [Fact]
        public void ValidateTool_WithValidTool_ReturnsNull()
        {
            // Arrange - Construct a valid tool DTO.
            var tool = new API.DTOs.Tools.Tool
            {
                Function = new API.DTOs.Tools.Function
                {
                    Name = "TestTool",
                    Description = "A test tool",
                    Parameters = new API.DTOs.Tools.Parameters
                    {
                        required = new string[] { "prop1" },
                        properties = new Dictionary<string, API.DTOs.Tools.Property>
                        {
                            {
                                "prop1", new API.DTOs.Tools.Property
                                {
                                    type = "string",
                                    description = "A required property"
                                }
                            }
                        }
                    }
                },
                ExecutionUrl = "http://example.com",
                ExecutionBase64Key = "key",
                ExecutionMethod = "GET"
            };

            // Act
            var result = _handler.ValidateTool(tool);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateTool_MissingFunctionName_ReturnsError()
        {
            // Arrange - Tool with null function name.
            var tool = new API.DTOs.Tools.Tool
            {
                Function = new API.DTOs.Tools.Function
                {
                    Name = null,
                    Description = "A test tool",
                    Parameters = new API.DTOs.Tools.Parameters
                    {
                        required = new string[] { },
                        properties = new Dictionary<string, API.DTOs.Tools.Property>()
                    }
                }
            };

            // Act
            var result = _handler.ValidateTool(tool);

            // Assert
            Assert.Equal("A function name is required for all tools.", result);
        }

        [Fact]
        public void ValidateTool_RequiredPropertyMissing_ReturnsError()
        {
            var tool = new API.DTOs.Tools.Tool
            {
                Function = new API.DTOs.Tools.Function
                {
                    Name = "TestTool",
                    Description = "A test tool",
                    Parameters = new API.DTOs.Tools.Parameters
                    {
                        required = new string[] { "prop1" },
                        properties = new Dictionary<string, API.DTOs.Tools.Property>()
                    }
                }
            };

            var result = _handler.ValidateTool(tool);

            Assert.Equal("Required property prop1 does not exist in the tool TestTool's properties list.", result);
        }

        [Fact]
        public void ValidateTool_DescriptionTooLong_ReturnsError()
        {
            var tool = new API.DTOs.Tools.Tool
            {
                Function = new API.DTOs.Tools.Function
                {
                    Name = "TestTool",
                    Description = new string('a', 513),
                    Parameters = new API.DTOs.Tools.Parameters
                    {
                        required = new string[] { },
                        properties = new Dictionary<string, API.DTOs.Tools.Property>()
                    }
                }
            };

            var result = _handler.ValidateTool(tool);

            Assert.Equal("The function description exceeds the maximum allowed length of 512 characters.", result);
        }

        #endregion

        #region ValidateMessageList and ValidateMessage Tests

        [Fact]
        public void ValidateMessageList_WithValidMessages_ReturnsNull()
        {
            // Arrange: Valid messages list (using a non-User role per current validation logic).
            var message = new Message
            {
                Role = Role.User,
                Content = "Hello world",
                User = "Tester"
            };

            var messages = new List<Message> { message };

            // Act
            var result = _handler.ValidateMessageList(messages);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateMessageList_WithEmptyList_ReturnsError()
        {
            // Arrange: Empty messages list.
            var messages = new List<Message>();

            // Act
            var result = _handler.ValidateMessageList(messages);

            // Assert
            Assert.Equal("The messages array was null or empty.", result);
        }

        [Fact]
        public void ValidateMessageList_NoUserMessage_ReturnsError()
        {
            var messages = new List<Message>
            {
                new Message { Role = Role.Assistant, Content = "test", User = "tester" }
            };

            var result = _handler.ValidateMessageList(messages);

            Assert.Equal("The messages array must contain at least one user message, but contains none.", result);
        }

        [Fact]
        public void ValidateMessage_WithoutContentOrImage_ReturnsError()
        {
            // Arrange: Message with neither content nor image.
            var message = new Message
            {
                Role = Role.Assistant,
                Content = "",
                Base64Image = null,
                User = "Tester"
            };

            // Act
            var result = _handler.ValidateMessage(message);

            // Assert
            Assert.Equal("All messages must contain content or an image.", result);
        }

        [Fact]
        public void ValidateMessage_WithValidContent_ReturnsNull()
        {
            // Arrange: A valid message with content.
            var message = new Message
            {
                Role = Role.Assistant,
                Content = "Some valid content",
                User = "Tester"
            };

            // Act
            var result = _handler.ValidateMessage(message);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateMessage_UserTooLong_ReturnsError()
        {
            var message = new Message
            {
                Role = Role.User,
                Content = "test",
                User = new string('a', 256)
            };

            var result = _handler.ValidateMessage(message);

            Assert.Equal("The user name exceeds the maximum allowed length of 255 characters.", result);
        }

        [Fact]
        public void ValidateMessage_InvalidBase64Image_ReturnsError()
        {
            var message = new Message
            {
                Role = Role.User,
                Content = "test",
                Base64Image = "notbase64",
                User = "tester"
            };

            var result = _handler.ValidateMessage(message);

            Assert.Equal("The image provided is not valid.", result);
        }

        #endregion

        #region ValidateIndexDefinition Tests

        [Fact]
        public void ValidateIndexDefinition_WithValidIndex_ReturnsNull()
        {
            // Arrange: Create a valid index metadata DTO.
            var index = new IndexMetadata
            {
                Name = "ValidIndex",
                IndexingInterval = TimeSpan.FromHours(1),
                EmbeddingModel = "embedding-model",
                RagHost = RagServiceHost.Azure,
                MaxRagAttachments = 20,
                ChunkOverlap = 0.5,
                GenerateKeywords = false,
                GenerateTopic = false,
                ScoringProfile = new IndexScoringProfile
                {
                    Name = "ScoreProfile",
                    SearchAggregation = SearchAggregation.Sum,
                    SearchInterpolation = SearchInterpolation.Linear,
                    FreshnessBoost = 0,
                    BoostDurationDays = 0,
                    TagBoost = 0,
                    Weights = new Dictionary<string, double> { { "key1", 1.0 } }
                }
            };

            // Act
            var result = _handler.ValidateIndexDefinition(index);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateIndexDefinition_WithInvalidName_ReturnsError()
        {
            // Arrange: Index with an invalid (whitespace) name.
            var index = new IndexMetadata
            {
                Name = "   ",
                IndexingInterval = TimeSpan.FromHours(1),
                MaxRagAttachments = 20,
                ChunkOverlap = 0.5
            };

            // Act
            var result = _handler.ValidateIndexDefinition(index);

            // Assert
            Assert.Equal("The provided index name is invalid.", result);
        }

        [Fact]
        public void ValidateIndexDefinition_WithWeaviateAndChunkOverlap_ReturnsError()
        {
            var index = new IndexMetadata
            {
                Name = "TestIndex",
                RagHost = RagServiceHost.Weaviate,
                GenerationHost = AGIServiceHost.OpenAI,
                IndexingInterval = TimeSpan.Zero,
                ChunkOverlap = 0.1
            };

            var result = _handler.ValidateIndexDefinition(index);

            Assert.Equal("ChunkOverlap is not supported when using the Weaviate RagHost.", result);
        }

        [Fact]
        public void ValidateIndexDefinition_WithWeaviateAndIndexingInterval_ReturnsError()
        {
            var index = new IndexMetadata
            {
                Name = "TestIndex",
                RagHost = RagServiceHost.Weaviate,
                GenerationHost = AGIServiceHost.OpenAI,
                IndexingInterval = TimeSpan.FromMinutes(5)
            };

            var result = _handler.ValidateIndexDefinition(index);

            Assert.Equal("IndexingInterval is not supported when using the Weaviate RagHost.", result);
        }

        [Fact]
        public void ValidateIndexDefinition_WithWeaviateAndScoringProfile_ReturnsError()
        {
            var index = new IndexMetadata
            {
                Name = "TestIndex",
                RagHost = RagServiceHost.Weaviate,
                GenerationHost = AGIServiceHost.OpenAI,
                IndexingInterval = TimeSpan.Zero,
                ScoringProfile = new IndexScoringProfile { Name = "Test" }
            };

            var result = _handler.ValidateIndexDefinition(index);

            Assert.Equal("Scoring profiles are not supported when using the Weaviate RagHost.", result);
        }

        [Fact]
        public void ValidateIndexDefinition_IndexingIntervalZero_ReturnsError()
        {
            var index = new IndexMetadata
            {
                Name = "Test",
                RagHost = RagServiceHost.Azure,
                IndexingInterval = TimeSpan.Zero,
                ChunkOverlap = 0.5
            };

            var result = _handler.ValidateIndexDefinition(index);

            Assert.Equal("IndexingInterval must be a positive value.", result);
        }

        [Fact]
        public void ValidateIndexDefinition_ChunkOverlapOutOfRange_ReturnsError()
        {
            var index = new IndexMetadata
            {
                Name = "Test",
                RagHost = RagServiceHost.Azure,
                IndexingInterval = TimeSpan.FromHours(1),
                ChunkOverlap = 1.5
            };

            var result = _handler.ValidateIndexDefinition(index);

            Assert.Equal("ChunkOverlap must be between 0 and 1 (inclusive).", result);
        }

        [Fact]
        public void ValidateIndexDefinition_InvalidEmbeddingModelForAzure_ReturnsError()
        {
            var index = new IndexMetadata
            {
                Name = "Test",
                RagHost = RagServiceHost.Azure,
                EmbeddingModel = DefaultWeaviateEmbeddingModel,
                IndexingInterval = TimeSpan.FromHours(1),
                ChunkOverlap = 0.5
            };

            var result = _handler.ValidateIndexDefinition(index);

            Assert.Equal($"EmbeddingModel {DefaultWeaviateEmbeddingModel} is reserved for the Weaviate RagHost.", result);
        }

        #endregion

        #region IsValidIndexName Tests

        [Fact]
        public void IsValidIndexName_WithValidName_ReturnsTrue()
        {
            // Arrange
            string validName = "Valid_TableName1";

            // Act
            var result = _handler.IsValidIndexName(validName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidIndexName_WithSqlKeyword_ReturnsFalse()
        {
            // Arrange: Using a SQL keyword should be rejected.
            string invalidName = "SELECT";

            // Act
            var result = _handler.IsValidIndexName(invalidName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidIndexName_WithInvalidCharacters_ReturnsFalse()
        {
            string invalidName = "Invalid-Name!";

            var result = _handler.IsValidIndexName(invalidName);

            Assert.False(result);
        }

        #endregion

        #region IsValidRagUpsertRequest Tests

        [Fact]
        public void IsValidRagUpsertRequest_WithValidDocuments_ReturnsNull()
        {
            // Arrange: Create a valid index document.
            var document = new IndexDocument
            {
                Title = "Doc title",
                Content = "Some content",
                Topic = "topic",
                Keywords = "keyword",
                Source = "http://source.com"
            };

            var request = new RagUpsertRequest
            {
                Documents = new List<IndexDocument> { document }
            };

            // Act
            var result = _handler.IsValidRagUpsertRequest(request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void IsValidRagUpsertRequest_WithNoDocuments_ReturnsError()
        {
            // Arrange: Create a request with no documents.
            var request = new RagUpsertRequest
            {
                Documents = new List<IndexDocument>()
            };

            // Act
            var result = _handler.IsValidRagUpsertRequest(request);

            // Assert
            Assert.Equal("The request must contain at least one document.", result);
        }

        [Fact]
        public void IsValidRagUpsertRequest_DocumentMissingTitle_ReturnsError()
        {
            var document = new IndexDocument
            {
                Title = "",
                Content = "content"
            };

            var request = new RagUpsertRequest
            {
                Documents = new List<IndexDocument> { document }
            };

            var result = _handler.IsValidRagUpsertRequest(request);

            Assert.Equal("Document title cannot be empty.", result);
        }

        #endregion
    }
}
