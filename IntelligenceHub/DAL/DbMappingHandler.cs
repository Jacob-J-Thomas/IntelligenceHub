﻿using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.API.DTOs.RAG;
using System.Text.Json;

namespace IntelligenceHub.DAL
{
    public static class DbMappingHandler
    {
        #region Profiles
        public static Profile MapFromDbProfile(DbProfile dbProfile, List<Tool>? tools = null)
        {
            var profile = new Profile()
            {
                Id = dbProfile.Id,
                Name = dbProfile.Name,
                Model = dbProfile.Model,
                Frequency_Penalty = dbProfile.FrequencyPenalty,
                Presence_Penalty = dbProfile.PresencePenalty,
                Temperature = dbProfile.Temperature,
                Top_P = dbProfile.TopP,
                Max_Tokens = dbProfile.MaxTokens,
                Top_Logprobs = dbProfile.TopLogprobs,
                Response_Format = dbProfile.ResponseFormat,
                User = dbProfile.User,
                System_Message = dbProfile.SystemMessage,
                Stop = dbProfile.Stop?.ToStringArray(),
                Reference_Profiles = dbProfile.ReferenceProfiles?.ToStringArray(),
                Tools = tools,
                MaxMessageHistory = dbProfile.MaxMessageHistory,
                Return_Recursion = dbProfile.ReturnRecursion,
            };
            profile.Logprobs = profile.Top_Logprobs > 0 ? true : false;
            return profile;
        }

        public static DbProfile MapToDbProfile(string profileName, DbProfile? existingProfile = null, Profile? profileUpdate = null)
        {
            return new DbProfile()
            {
                // update or set existing value
                Id = existingProfile?.Id ?? 0,
                Name = profileName, // this value should not be null when this method is called, so it is required as an argument
                ResponseFormat = profileUpdate?.Response_Format ?? existingProfile?.ResponseFormat,
                User = profileUpdate?.User ?? existingProfile?.User,
                SystemMessage = profileUpdate?.System_Message ?? existingProfile?.SystemMessage,
                TopLogprobs = profileUpdate?.Top_Logprobs ?? existingProfile?.TopLogprobs,

                // Variables with default values during first database entry
                Model = profileUpdate?.Model
                    ?? existingProfile?.Model
                    ?? "gpt-4o-mini",

                FrequencyPenalty = profileUpdate?.Frequency_Penalty
                    ?? existingProfile?.FrequencyPenalty
                    ?? 0,

                PresencePenalty = profileUpdate?.Presence_Penalty
                    ?? existingProfile?.PresencePenalty
                    ?? 0,

                Temperature = profileUpdate?.Temperature
                    ?? existingProfile?.Temperature
                    ?? 1,

                TopP = profileUpdate?.Top_P
                    ?? existingProfile?.TopP
                    ?? 1,

                MaxTokens = profileUpdate?.Max_Tokens
                    ?? existingProfile?.MaxTokens
                    ?? 1200,

                ReturnRecursion = profileUpdate?.Return_Recursion
                    ?? existingProfile?.ReturnRecursion
                    ?? false,

                Stop = profileUpdate?.Stop?.ToCommaSeparatedString() ?? existingProfile?.Stop,
                ReferenceProfiles = profileUpdate?.Reference_Profiles?.ToCommaSeparatedString() ?? existingProfile?.ReferenceProfiles,
            };
        }
        #endregion

        #region Tools
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
                    Name = dbTool.Name,
                    Description = dbTool.Description
                }
            };

            if (dbProperties == null) dbProperties = new List<DbProperty>(); // To prevent null reference exceptions
            foreach (var property in dbProperties) 
            {
                var convertedProp = new Property()
                {
                    Id = property.Id,
                    Type = property.Type,
                    Description = property.Description,
                };
                tool.Function.Parameters.Properties.Add(property.Name, convertedProp);
                tool.Function.Parameters.Required = dbTool.Required.ToStringArray();
            }
            return tool;
        }

        public static DbTool MapToDbTool(Tool tool)
        {
            return new DbTool()
            {
                Id = tool.Id,
                Name = tool.Function.Name,
                Description = tool.Function.Description ?? string.Empty,
                Required = tool.Function.Parameters.Required?.ToCommaSeparatedString() ?? string.Empty,
                ExecutionUrl = tool.ExecutionUrl,
                ExecutionMethod = tool.ExecutionMethod,
                ExecutionBase64Key = tool.ExecutionBase64Key,
            };
        }

        public static DbProperty MapToDbProperty(string name, Property property)
        {
            return new DbProperty()
            {
                Id = property.Id ?? 0,
                ToolId = property.Id ?? 0,
                Name = name,
                Type = property.Type,
                Description = property.Description ?? string.Empty,
            };
        }
        #endregion

        #region Messaging
        public static Message MapFromDbMessage(DbMessage dbMessage)
        {
            return new Message()
            {
                Content = dbMessage.Content,
                Role = dbMessage.Role.ConvertStringToRole(),
                Base64Image = dbMessage.Base64Image,
                TimeStamp = dbMessage.TimeStamp,
            };
        }

        public static DbMessage MapToDbMessage(Message message, Guid conversationId, string[]? toolsCalled = null)
        {
            return new DbMessage()
            {
                Content = message.Content,
                Role = message.Role.ToString() ?? string.Empty,
                ConversationId = conversationId,
                Base64Image = message.Base64Image,
                TimeStamp = message.TimeStamp,
            };
        }
        #endregion

        #region RAG Indexing

        public static IndexDocument MapFromDbIndexDocument(DbIndexDocument dbDocument)
        {
            return new IndexDocument()
            {
                Title = dbDocument.Title,
                Content = dbDocument.Content,
                Topic = dbDocument.Topic,
                Created = dbDocument.Created,
                Modified = dbDocument.Modified
            };
        }

        public static DbIndexDocument MapToDbIndexDocument(IndexDocument document)
        {
            return new DbIndexDocument()
            {
                Title = document.Title,
                Content = document.Content,
                Topic = document.Topic,
                Created = document.Created,
                Modified = document.Modified
            };
        }

        public static IndexMetadata MapFromDbIndexMetadata(DbIndexMetadata dbIndexData)
        {
            return new IndexMetadata()
            {
                Name = dbIndexData.Name,
                QueryType = dbIndexData.QueryType,
                ChunkOverlap = dbIndexData.ChunkOverlap,
                IndexingInterval = dbIndexData.IndexingInterval,
                MaxRagAttachments = dbIndexData.MaxRagAttachments,
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
                    Aggregation = dbIndexData.ScoringAggregation,
                    Interpolation = dbIndexData.ScoringInterpolation,
                    BoostDurationDays = dbIndexData.ScoringBoostDurationDays,
                    FreshnessBoost = dbIndexData.ScoringFreshnessBoost,
                    TagBoost = dbIndexData.ScoringTagBoost,
                    Weights = DeserializeDbWeights(dbIndexData.ScoringWeights)
                }
            };
        }

        public static DbIndexMetadata MapToDbIndexMetadata(IndexMetadata indexData)
        {
            return new DbIndexMetadata()
            {
                Name = indexData.Name,
                QueryType = indexData.QueryType,
                ChunkOverlap = indexData.ChunkOverlap,
                IndexingInterval = indexData.IndexingInterval,
                MaxRagAttachments = indexData.MaxRagAttachments,
                EmbeddingModel = indexData.EmbeddingModel,
                GenerateTopic = indexData.GenerateTopic,
                GenerateKeywords = indexData.GenerateKeywords,
                GenerateTitleVector = indexData.GenerateTitleVector,
                GenerateContentVector = indexData.GenerateContentVector,
                GenerateTopicVector = indexData.GenerateTopicVector,
                GenerateKeywordVector = indexData.GenerateKeywordVector,
                DefaultScoringProfile = indexData.ScoringProfile?.Name,
                ScoringAggregation = indexData.ScoringProfile?.Aggregation,
                ScoringInterpolation = indexData.ScoringProfile?.Interpolation,
                ScoringFreshnessBoost = indexData.ScoringProfile?.FreshnessBoost ?? 0,
                ScoringBoostDurationDays = indexData.ScoringProfile?.BoostDurationDays ?? 0,
                ScoringTagBoost = indexData.ScoringProfile?.TagBoost ?? 0,
                ScoringWeights = indexData.ScoringProfile.Weights.Any() ? SerializeDbWeights(indexData.ScoringProfile.Weights) : string.Empty,
            };
        }

        private static Dictionary<string, double>? DeserializeDbWeights(string serializedWeights)
        {
            return JsonSerializer.Deserialize<Dictionary<string, double>>(serializedWeights) ?? null;
        }

        private static string SerializeDbWeights(Dictionary<string, double> weights)
        {
            return JsonSerializer.Serialize(weights);
        }

        #endregion
    }
}