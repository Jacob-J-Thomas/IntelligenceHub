﻿CREATE TABLE IndexMetadata (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(255) UNIQUE NOT NULL ,
    QueryType NVARCHAR(255),
    IndexingInterval TIME NOT NULL,
    EmbeddingModel NVARCHAR(255),
    MaxRagAttachments INT,
    ChunkOverlap FLOAT,
    GenerateTopic BIT,
    GenerateKeywords BIT,
    GenerateTitleVector BIT,
    GenerateContentVector BIT,
    GenerateTopicVector BIT,
    GenerateKeywordVector BIT,
    DefaultScoringProfile NVARCHAR(255),
    ScoringAggregation NVARCHAR(255),
    ScoringInterpolation NVARCHAR(255),
    ScoringFreshnessBoost FLOAT,
    ScoringBoostDurationDays INT,
    ScoringTagBoost FLOAT,
    ScoringWeights NVARCHAR(MAX)
);