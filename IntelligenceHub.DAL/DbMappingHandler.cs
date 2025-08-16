﻿using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.API.DTOs.RAG;
using System.Text.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.DAL
{
    /// <summary>
    /// Handles mapping between database entities and DTOs and sets defaults where appropriate.
    /// </summary>
    public static class DbMappingHandler
    {
        /// <summary>
        /// Appends the tenant id to an entity name. If the name already
        /// contains the tenant prefix it is returned unchanged.
        /// </summary>
        public static string AppendTenantToName(string name, Guid tenantId)
        {
            var prefix = $"{tenantId}_";
            return name.StartsWith(prefix) ? name : prefix + name;
        }

        /// <summary>
        /// Removes the tenant id prefix from an entity name if present.
        /// </summary>
        public static string RemoveTenantFromName(string name, Guid tenantId)
        {
            var prefix = $"{tenantId}_";
            return name.StartsWith(prefix) ? name[prefix.Length..] : name;
        }

        #region Profiles

        /// <summary>
        /// Maps a database profile entity to a profile DTO.
        /// </summary>
        /// <param name="dbProfile">The database profile entity.</param>
        /// <returns>A profile DTO.</returns>
        public static Profile MapFromDbProfile(DbProfile dbProfile)
        {
            var profile = new Profile()
            {
                Id = dbProfile.Id,
                Name = RemoveTenantFromName(dbProfile.Name, dbProfile.TenantId),
                Model = dbProfile.Model,
                Host = dbProfile.Host.ConvertToServiceHost(),
                ImageHost = dbProfile.ImageHost?.ConvertToServiceHost(),
                RagDatabase = dbProfile.RagDatabase,
                FrequencyPenalty = (float?)dbProfile.FrequencyPenalty,
                PresencePenalty = (float?)dbProfile.PresencePenalty,
                Temperature = (float?)dbProfile.Temperature,
                TopP = (float?)dbProfile.TopP,
                MaxTokens = dbProfile.MaxTokens,
                TopLogprobs = dbProfile.TopLogprobs,
                ResponseFormat = dbProfile.ResponseFormat,
                User = dbProfile.User,
                SystemMessage = dbProfile.SystemMessage,
                Stop = dbProfile.Stop?.ToStringArray(),
                ReferenceProfiles = dbProfile.ReferenceProfiles?.ToStringArray(),
                Tools = dbProfile.ProfileTools.Select(pt => new Tool 
                { 
                    Id = pt.Tool.Id, 
                    Function = new Function() 
                    { 
                        Name = RemoveTenantFromName(pt.Tool.Name, pt.Tool.TenantId),
                        Description = pt.Tool.Description, 
                        Parameters = new Parameters() 
                        { 
                            type = "object", 
                            required = pt.Tool.Required.ToStringArray(),  
                            properties = pt.Tool.Properties.ToDictionary(p => RemoveTenantFromName(p.Name, p.TenantId), p => new Property()
                            { 
                                Id = p.Id, 
                                type = p.Type, 
                                description = p.Description 
                            }) 
                        } 
                    }, 
                    ExecutionUrl = pt.Tool.ExecutionUrl, 
                    ExecutionBase64Key = pt.Tool.ExecutionBase64Key, 
                    ExecutionMethod = pt.Tool.ExecutionMethod 
                }).ToList(),
                MaxMessageHistory = dbProfile.MaxMessageHistory,
            };
            profile.Logprobs = profile.TopLogprobs > 0 ? true : false;

            profile.Name = profile.Name.RemoveTenant();
            if (profile.Tools != null)
            {
                foreach (var t in profile.Tools)
                {
                    t.Function.Name = t.Function.Name.RemoveTenant();
                }
            }

            return profile;
        }

        /// <summary>
        /// Maps a profile DTO to a database profile entity.
        /// </summary>
        /// <param name="profileName">The name of the profile.</param>
        /// /// <param name="defaultAzureModel">The default azure model, passed in since this 
        /// class is static, and the value is retrieved from the appsettings.</param>
        /// <param name="existingProfile">Optional existing database profile entity.</param>
        /// <param name="profileUpdate">Optional profile DTO with updated values.</param>
        /// <returns>A database profile entity.</returns>
        public static DbProfile MapToDbProfile(string profileName, string defaultAzureModel, Guid? tenantId, DbProfile? existingProfile = null, Profile? profileUpdate = null)
        {
            if (existingProfile == null) existingProfile = new DbProfile();

            var host = profileUpdate?.Host ?? existingProfile.Host.ConvertToServiceHost();
            if (host == AGIServiceHost.None) host = AGIServiceHost.OpenAI;

            var model = profileUpdate?.Model ?? existingProfile.Model ?? null;
            if (string.IsNullOrEmpty(model))
            {
                if (host == AGIServiceHost.Azure) model = defaultAzureModel;
                if (host == AGIServiceHost.Anthropic) model = DefaultAnthropicModel;
                if (host == AGIServiceHost.OpenAI) model = DefaultOpenAIModel;
            }

            existingProfile.Name = profileName.AppendTenant(tenantId);
            existingProfile.ResponseFormat = profileUpdate?.ResponseFormat ?? existingProfile.ResponseFormat;
            existingProfile.User = profileUpdate?.User ?? existingProfile.User;
            existingProfile.SystemMessage = profileUpdate?.SystemMessage ?? existingProfile.SystemMessage;
            existingProfile.RagDatabase = profileUpdate?.RagDatabase ?? existingProfile.RagDatabase;
            existingProfile.TopLogprobs = profileUpdate?.TopLogprobs ?? existingProfile.TopLogprobs;
            existingProfile.MaxTokens = profileUpdate?.MaxTokens ?? existingProfile.MaxTokens;
            existingProfile.Model = model;
            existingProfile.Host = host.ToString();
            existingProfile.ImageHost = profileUpdate?.ImageHost.ToString() ?? existingProfile.ImageHost ?? host.ToString();
            existingProfile.FrequencyPenalty = profileUpdate?.FrequencyPenalty ?? existingProfile.FrequencyPenalty ?? 0;
            existingProfile.PresencePenalty = profileUpdate?.PresencePenalty ?? existingProfile.PresencePenalty ?? 0;
            existingProfile.Temperature = profileUpdate?.Temperature ?? existingProfile.Temperature ?? 1;
            existingProfile.TopP = profileUpdate?.TopP ?? existingProfile.TopP ?? 1;
            existingProfile.Stop = profileUpdate?.Stop?.ToCommaSeparatedString() ?? existingProfile.Stop;
            existingProfile.ReferenceProfiles = profileUpdate?.ReferenceProfiles?.ToCommaSeparatedString() ?? existingProfile.ReferenceProfiles;

            return existingProfile;
        }
        #endregion

        #region Tools

        /// <summary>
        /// Maps a database tool entity to a tool DTO.
        /// </summary>
        /// <param name="dbTool">The database tool entity.</param>
        /// <param name="dbProperties">Optional list of database properties associated with the tool.</param>
        /// <returns>A tool DTO.</returns>
        public static Tool MapFromDbTool(DbTool dbTool, List<DbProperty>? dbProperties = null)
        {
            var tool = new Tool()
            {
                Id = dbTool.Id,
                ExecutionUrl = dbTool.ExecutionUrl,
                ExecutionMethod = dbTool.ExecutionMethod,
                ExecutionBase64Key = dbTool.ExecutionBase64Key,
                Function = new Function()
                {
                    Name = RemoveTenantFromName(dbTool.Name, dbTool.TenantId),
                    Description = dbTool.Description
                }
            };

            if (dbProperties == null) dbProperties = new List<DbProperty>(); // To prevent null reference exceptions
            foreach (var property in dbProperties)
            {
                var convertedProp = new Property()
                {
                    Id = property.Id,
                    type = property.Type,
                    description = property.Description,
                };
                tool.Function.Parameters.properties.Add(RemoveTenantFromName(property.Name, property.TenantId), convertedProp);
                tool.Function.Parameters.required = dbTool.Required.ToStringArray();
            }

            tool.Function.Name = tool.Function.Name.RemoveTenant();
            return tool;
        }

        /// <summary>
        /// Maps a tool DTO to a database tool entity.
        /// </summary>
        /// <param name="tool">The tool DTO.</param>
        /// <returns>A database tool entity.</returns>
        public static DbTool MapToDbTool(Tool tool, Guid? tenantId)
        {
            return new DbTool()
            {
                Id = tool.Id,
                Name = tool.Function.Name.AppendTenant(tenantId),
                Description = tool.Function.Description ?? string.Empty,
                Required = tool.Function.Parameters.required?.ToCommaSeparatedString() ?? string.Empty,
                ExecutionUrl = tool.ExecutionUrl,
                ExecutionMethod = tool.ExecutionMethod,
                ExecutionBase64Key = tool.ExecutionBase64Key,
            };
        }

        /// <summary>
        /// Maps a property DTO to a database property entity.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="property">The property DTO.</param>
        /// <returns>A database property entity.</returns>
        public static DbProperty MapToDbProperty(string name, Property property)
        {
            return new DbProperty()
            {
                Id = property.Id ?? 0,
                Name = name,
                Type = property.type,
                Description = property.description ?? string.Empty,
            };
        }
        #endregion

        #region Messaging

        /// <summary>
        /// Maps a database message entity to a message DTO.
        /// </summary>
        /// <param name="dbMessage">The database message entity.</param>
        /// <returns>A message DTO.</returns>
        public static Message MapFromDbMessage(DbMessage dbMessage)
        {
            return new Message()
            {
                Id = dbMessage.Id,
                Content = dbMessage.Content,
                Role = dbMessage.Role.ConvertStringToRole(),
                User = dbMessage.User,
                Base64Image = dbMessage.Base64Image,
                TimeStamp = dbMessage.TimeStamp,
            };
        }

        /// <summary>
        /// Maps a message DTO to a database message entity.
        /// </summary>
        /// <param name="message">The message DTO.</param>
        /// <param name="conversationId">The conversation ID.</param>
        /// <returns>A database message entity.</returns>
        public static DbMessage MapToDbMessage(Message message, Guid conversationId)
        {
            return new DbMessage()
            {
                Id = message.Id,
                Content = message.Content,
                Role = message.Role.ToString() ?? string.Empty,
                ConversationId = conversationId,
                User = message.User ?? string.Empty,
                Base64Image = message.Base64Image,
                TimeStamp = message.TimeStamp,
            };
        }
        #endregion

        #region RAG Indexing

        /// <summary>
        /// Maps a database index document entity to an index document DTO.
        /// </summary>
        /// <param name="dbDocument">The database index document entity.</param>
        /// <returns>An index document DTO.</returns>
        public static IndexDocument MapFromDbIndexDocument(DbIndexDocument dbDocument)
        {
            return new IndexDocument()
            {
                Id = dbDocument.Id,
                Title = dbDocument.Title,
                Content = dbDocument.Content,
                Topic = dbDocument.Topic,
                Keywords = dbDocument.Keywords,
                Source = dbDocument.Source,
                Created = dbDocument.Created,
                Modified = dbDocument.Modified
            };
        }

        /// <summary>
        /// Maps an index document DTO to a database index document entity.
        /// </summary>
        /// <param name="document">The index document DTO.</param>
        /// <returns>A database index document entity.</returns>
        public static DbIndexDocument MapToDbIndexDocument(IndexDocument document)
        {
            return new DbIndexDocument()
            {
                Id = document.Id,
                Title = document.Title,
                Content = document.Content,
                Topic = document.Topic,
                Keywords = document.Keywords,
                Source = document.Source,
                Created = document.Created,
                Modified = document.Modified
            };
        }

        /// <summary>
        /// Maps a database index metadata entity to an index metadata DTO.
        /// </summary>
        /// <param name="dbIndexData">The database index metadata entity.</param>
        /// <returns>An index metadata DTO.</returns>
        public static IndexMetadata MapFromDbIndexMetadata(DbIndexMetadata dbIndexData)
        {
            var metadata = new IndexMetadata()
            {
                Name = RemoveTenantFromName(dbIndexData.Name, dbIndexData.TenantId),
                QueryType = dbIndexData.QueryType?.ConvertStringToQueryType() ?? QueryType.Simple,
                GenerationHost = dbIndexData.GenerationHost.ConvertToServiceHost(),
                RagHost = dbIndexData.RagHost.ConvertToRagHost(),
                ChunkOverlap = dbIndexData.ChunkOverlap ?? (dbIndexData.RagHost.ConvertToRagHost() == RagServiceHost.Weaviate ? 0 : DefaultChunkOverlap),
                IndexingInterval = dbIndexData.IndexingInterval,
                MaxRagAttachments = dbIndexData.MaxRagAttachments ?? DefaultRagAttachmentNumber, // make this a global variable
                EmbeddingModel = dbIndexData.EmbeddingModel,
                GenerateTopic = dbIndexData.GenerateTopic,
                GenerateKeywords = dbIndexData.GenerateKeywords,
                GenerateTitleVector = dbIndexData.GenerateTitleVector,
                GenerateContentVector = dbIndexData.GenerateContentVector,
                GenerateTopicVector = dbIndexData.GenerateTopicVector,
                GenerateKeywordVector = dbIndexData.GenerateKeywordVector,
                ScoringProfile = new IndexScoringProfile()
                {
                    Name = dbIndexData.DefaultScoringProfile ?? string.Empty,
                    SearchAggregation = dbIndexData.ScoringAggregation?.ConvertStringToSearchAggregation(),
                    SearchInterpolation = dbIndexData.ScoringInterpolation?.ConvertStringToSearchInterpolation(),
                    BoostDurationDays = dbIndexData.ScoringBoostDurationDays ?? DefaultScoringBoostDurationDays,
                    FreshnessBoost = dbIndexData.ScoringFreshnessBoost ?? DefaultScoringFreshnessBoost,
                    TagBoost = dbIndexData.ScoringTagBoost ?? DefaultScoringTagBoost,
                    Weights = DeserializeDbWeights(dbIndexData.ScoringWeights) ?? new Dictionary<string, double>()
                }
            };

            metadata.Name = metadata.Name.RemoveTenant();
            return metadata;
        }

        /// <summary>
        /// Maps an index metadata DTO to a database index metadata entity.
        /// </summary>
        /// <param name="indexData">The index metadata DTO.</param>
        /// <returns>A database index metadata entity.</returns>
        public static DbIndexMetadata MapToDbIndexMetadata(IndexMetadata indexData, Guid? tenantId)
        {
            var defaultEmbeddingModel = indexData.RagHost == RagServiceHost.Azure ? DefaultAzureSearchEmbeddingModel : DefaultWeaviateEmbeddingModel;
            if (indexData.ScoringProfile == null || indexData.RagHost == RagServiceHost.Weaviate) indexData.ScoringProfile = new IndexScoringProfile();
            var defaultChunkOverlap = indexData.RagHost == RagServiceHost.Weaviate ? 0 : DefaultChunkOverlap;
            var defaultIndexingInterval = indexData.RagHost == RagServiceHost.Weaviate ? TimeSpan.Zero : TimeSpan.FromHours(23.99);
            var chunkOverlap = indexData.ChunkOverlap ?? defaultChunkOverlap;
            return new DbIndexMetadata()
            {
                Name = indexData.Name.AppendTenant(tenantId),
                QueryType = indexData.QueryType.ToString(),
                GenerationHost = indexData.GenerationHost.ToString() ?? AGIServiceHost.None.ToString(),
                RagHost = indexData.RagHost.ToString() ?? RagServiceHost.None.ToString(),
                ChunkOverlap = chunkOverlap,
                IndexingInterval = indexData.IndexingInterval ?? defaultIndexingInterval,
                MaxRagAttachments = indexData.MaxRagAttachments ?? DefaultRagAttachmentNumber, // make this a global variable,
                EmbeddingModel = indexData.EmbeddingModel ?? defaultEmbeddingModel,
                GenerateTopic = indexData.GenerateTopic ?? false,
                GenerateKeywords = indexData.GenerateKeywords ?? false,
                GenerateTitleVector = indexData.GenerateTitleVector ?? true,
                GenerateContentVector = indexData.GenerateContentVector ?? true,
                GenerateTopicVector = indexData.GenerateTopicVector ?? false,
                GenerateKeywordVector = indexData.GenerateKeywordVector ?? false,
                DefaultScoringProfile = indexData.ScoringProfile?.Name ?? DefaultVectorScoringProfile,
                ScoringAggregation = indexData.ScoringProfile?.SearchAggregation.ToString(),
                ScoringInterpolation = indexData.ScoringProfile?.SearchInterpolation.ToString(),
                ScoringFreshnessBoost = indexData.ScoringProfile?.FreshnessBoost ?? DefaultScoringFreshnessBoost,
                ScoringBoostDurationDays = indexData.ScoringProfile?.BoostDurationDays ?? DefaultScoringBoostDurationDays,
                ScoringTagBoost = indexData.ScoringProfile?.TagBoost ?? DefaultScoringTagBoost,
                ScoringWeights = indexData.ScoringProfile?.Weights?.Count > 0 ? SerializeDbWeights(indexData.ScoringProfile.Weights) : string.Empty,
            };
        }

        /// <summary>
        /// Deserializes a JSON string to a dictionary of weights.
        /// </summary>
        /// <param name="serializedWeights">The JSON string representing the weights.</param>
        /// <returns>A dictionary of weights.</returns>
        private static Dictionary<string, double>? DeserializeDbWeights(string? serializedWeights)
        {
            if (string.IsNullOrEmpty(serializedWeights)) return null;
            return JsonSerializer.Deserialize<Dictionary<string, double>>(serializedWeights) ?? null;
        }

        /// <summary>
        /// Serializes a dictionary of weights to a JSON string.
        /// </summary>
        /// <param name="weights">The dictionary of weights.</param>
        /// <returns>A JSON string representing the weights.</returns>
        private static string SerializeDbWeights(Dictionary<string, double> weights)
        {
            return JsonSerializer.Serialize(weights);
        }

        #endregion
    }
}
