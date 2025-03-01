﻿using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.API.DTOs.Tools;

namespace IntelligenceHub.Business.Handlers
{
    public interface IValidationHandler
    {

        public string? ValidateChatRequest(CompletionRequest chatRequest);
        public string? ValidateAPIProfile(Profile profile);
        public string? ValidateBaseDTO(Profile profile);
        public string? ValidateTool(Tool tool);
        public string? ValidateProperties(Dictionary<string, Property> properties);
        public string? ValidateIndexDefinition(IndexMetadata index);
        public bool IsValidIndexName(string tableName);
        public string? IsValidRagUpsertRequest(RagUpsertRequest documentName);
    }
}
