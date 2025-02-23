using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Client.Implementations
{
    public class AISearchServiceClient : IAISearchServiceClient
    {
        private readonly SearchIndexClient _indexClient;
        private readonly SearchIndexerClient _indexerClient;

        private readonly string _sqlRagDbConnectionString;
        private readonly string _openaiKey;
        private readonly string _openaiUrl;

        private readonly int _defaultRagDimensions = 3072; // move to globalvariables

        // Rag indexes are created with lower case values
        private enum RagField
        {
            Title,
            Content,
            Topic,
            Keywords,
            Created,
            Modified,
            Source,
            Chunk,
            ContentVector,
            ContentEmbedding,
            KeywordVector,
            KeywordEmbedding,
            TopicVector,
            TopicEmbedding,
            TitleVector,
            TitleEmbedding,
            TextItems,
            Pages,
            ChunkSplits,
        }

        private enum RagFieldType
        {
            text,
            textItems,
            embedding
        }

        public AISearchServiceClient(IOptionsMonitor<SearchServiceClientSettings> searchClientSettings, IOptionsMonitor<AGIClientSettings> agiClientSettings, IOptionsMonitor<Settings> settings)
        {
            var credential = new AzureKeyCredential(searchClientSettings.CurrentValue.Key);

            var options = new SearchClientOptions()
            {
                RetryPolicy = new RetryPolicy(5, DelayStrategy.CreateExponentialDelayStrategy(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(120)))
            };

            _indexClient = new SearchIndexClient(new Uri(searchClientSettings.CurrentValue.Endpoint), credential, options);
            _indexerClient = new SearchIndexerClient(new Uri(searchClientSettings.CurrentValue.Endpoint), credential, options);

            _sqlRagDbConnectionString = settings.CurrentValue.DbConnectionString;
            _openaiUrl = agiClientSettings.CurrentValue.SearchServiceCompletionServiceEndpoint;
            _openaiKey = agiClientSettings.CurrentValue.SearchServiceCompletionServiceKey;
        }

        // index operations
        public async Task<List<string>> GetAllIndexNames()
        {
            var indexNames = new List<string>();
            var responseCollection = _indexClient.GetIndexNamesAsync();

            await foreach (var response in responseCollection) indexNames.Add(response);
            return indexNames;
        }

        public async Task<SearchResults<IndexDefinition>> SearchIndex(IndexMetadata index, string query)
        {
            var searchClient = _indexClient.GetSearchClient(index.Name);

            var queryType = SearchQueryType.Simple;
            if (index.QueryType.ToString()?.ToLower() == SearchQueryType.Full.ToString().ToLower()) queryType = SearchQueryType.Full;
            else if (index.QueryType.ToString()?.ToLower() == SearchQueryType.Semantic.ToString().ToLower()) queryType = SearchQueryType.Semantic;

            var vectorProfile = new VectorSearchOptions();
            var vectorizableQuery = new VectorizableTextQuery(query);
            vectorizableQuery.Fields.Add("contentVector");
            vectorizableQuery.Fields.Add("titleVector");
            vectorizableQuery.Fields.Add("topicVector");
            vectorizableQuery.Fields.Add("keywordsVector");

            vectorProfile.Queries.Add(vectorizableQuery);

            var options = new SearchOptions()
            {
                ScoringProfile = index.ScoringProfile?.Name,
                Size = index.MaxRagAttachments,
                QueryType = queryType,
                SemanticSearch = new SemanticSearchOptions()
                {
                    ErrorMode = SemanticErrorMode.Partial,
                    SemanticConfigurationName = SearchQueryType.Semantic.ToString()
                },
                VectorSearch = vectorProfile
            };

            options.HighlightFields.Add(RagField.Chunk.ToString().ToLower());

            options.SearchFields.Add(RagField.Chunk.ToString().ToLower());
            options.SearchFields.Add(RagField.Title.ToString().ToLower());
            options.SearchFields.Add(RagField.Topic.ToString().ToLower());
            options.SearchFields.Add(RagField.Keywords.ToString().ToLower());

            var response = await searchClient.SearchAsync<IndexDefinition>(query, options);
            return response.Value;
        }

        public async Task<bool> UpsertIndex(IndexMetadata indexDefinition)
        {
            var searchIndex = GetIndexDefinition(indexDefinition);

            if (indexDefinition.ScoringProfile != null)
            {
                if (string.IsNullOrEmpty(indexDefinition.ScoringProfile.Name)) indexDefinition.ScoringProfile.Name = DefaultVectorSearchProfile;
                var scoringProfile = new ScoringProfile(indexDefinition.ScoringProfile?.Name);
                if (indexDefinition.ScoringProfile?.Weights != null && indexDefinition.ScoringProfile.Weights.Any()) scoringProfile.TextWeights = new TextWeights(indexDefinition.ScoringProfile.Weights);

                // assign function aggregation
                if (indexDefinition.ScoringProfile?.SearchAggregation.ToString()?.ToLower() == ScoringFunctionAggregation.Sum.ToString().ToLower()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Sum;
                else if (indexDefinition.ScoringProfile?.SearchAggregation.ToString()?.ToLower() == ScoringFunctionAggregation.FirstMatching.ToString().ToLower()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.FirstMatching;
                else if (indexDefinition.ScoringProfile?.SearchAggregation.ToString()?.ToLower() == ScoringFunctionAggregation.Average.ToString().ToLower()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Average;
                else if (indexDefinition.ScoringProfile?.SearchAggregation.ToString()?.ToLower() == ScoringFunctionAggregation.Minimum.ToString().ToLower()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Minimum;
                else if (indexDefinition.ScoringProfile?.SearchAggregation.ToString()?.ToLower() == ScoringFunctionAggregation.Maximum.ToString().ToLower()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Maximum;

                // assign interpolation
                var interpolation = ScoringFunctionInterpolation.Linear;
                if (indexDefinition.ScoringProfile?.SearchInterpolation.ToString()?.ToLower() == ScoringFunctionInterpolation.Constant.ToString().ToLower()) interpolation = ScoringFunctionInterpolation.Constant;
                else if (indexDefinition.ScoringProfile?.SearchInterpolation.ToString()?.ToLower() == ScoringFunctionInterpolation.Quadratic.ToString().ToLower()) interpolation = ScoringFunctionInterpolation.Quadratic;
                else if (indexDefinition.ScoringProfile?.SearchInterpolation.ToString()?.ToLower() == ScoringFunctionInterpolation.Logarithmic.ToString().ToLower()) interpolation = ScoringFunctionInterpolation.Logarithmic;

                // add scoring functions to the profile
                if (indexDefinition.ScoringProfile?.FreshnessBoost > 1) scoringProfile.Functions.Add(new FreshnessScoringFunction(RagField.Modified.ToString(), indexDefinition.ScoringProfile.FreshnessBoost, new FreshnessScoringParameters(TimeSpan.FromDays(indexDefinition.ScoringProfile.BoostDurationDays)))
                {
                    Interpolation = interpolation,
                });
                else if (indexDefinition.ScoringProfile?.TagBoost > 1)
                {
                    scoringProfile.Functions.Add(new TagScoringFunction(RagField.Title.ToString(), indexDefinition.ScoringProfile.TagBoost, new TagScoringParameters(RagField.Keywords.ToString()))
                    {
                        Interpolation = interpolation
                    });
                    scoringProfile.Functions.Add(new TagScoringFunction(RagField.Content.ToString(), indexDefinition.ScoringProfile.TagBoost, new TagScoringParameters(RagField.Keywords.ToString()))
                    {
                        Interpolation = interpolation
                    });
                }
                searchIndex.ScoringProfiles.Add(scoringProfile);
            }

            var response = await _indexClient.CreateOrUpdateIndexAsync(searchIndex);
            return true;
        }

        public async Task<bool> DeleteIndex(string indexName)
        {
            var response = await _indexClient.DeleteIndexAsync(indexName);
            return response.Status > 199 && response.Status < 300;
        }

        public async Task<bool> DeleteIndexer(string indexName, string embeddingModel)
        {
            var skillsetDeletionResponse = await _indexerClient.DeleteSkillsetAsync(indexName.ToLower() + "-skillset");
            if (skillsetDeletionResponse == null || skillsetDeletionResponse.IsError) return false;

            var response = await _indexerClient.DeleteIndexerAsync(indexName + "-indexer");
            return response.Status > 199 && response.Status < 300;
        }

        public async Task<bool> CreateDatasource(string databaseName)
        {
            try
            {
                var container = new SearchIndexerDataContainer(databaseName);
                var connection = new SearchIndexerDataSourceConnection(databaseName, SearchIndexerDataSourceType.AzureSql, _sqlRagDbConnectionString, container)
                {
                    DataChangeDetectionPolicy = new SqlIntegratedChangeTrackingPolicy(),
                };
                var response = await _indexerClient.CreateDataSourceConnectionAsync(connection);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return false;
            }
        }

        public async Task<bool> DeleteDatasource(string indexName)
        {
            var response = await _indexerClient.DeleteDataSourceConnectionAsync(indexName);
            return response.Status > 199 && response.Status < 300;
        }

        public async Task<bool> RunIndexer(string indexName)
        {
            var response = await _indexerClient.RunIndexerAsync(indexName + "-indexer");
            return response.Status > 199 && response.Status < 300;
        }

        public async Task<bool> UpsertIndexer(IndexMetadata index)
        {
            // Define the indexer
            var interval = TimeSpan.FromDays(1);
            if (index.IndexingInterval.HasValue) interval = index.IndexingInterval.Value;
            var indexer = new SearchIndexer($"{index.Name}-indexer", index.Name, index.Name) { Schedule = new IndexingSchedule(interval) };

            // Map the existing "Id" column to the parent_id and chunk_id fields
            indexer.FieldMappings.Add(new FieldMapping("Id") { TargetFieldName = "parent_id" });
            indexer.FieldMappings.Add(new FieldMapping("Id") { TargetFieldName = "chunk_id" });

            indexer.SkillsetName = await UpsertSkillset(index);
            await _indexerClient.CreateOrUpdateIndexerAsync(indexer);

            return true;
        }

        private async Task<string> UpsertSkillset(IndexMetadata index)
        {
            var chunkingLengthInChars = 1312; // (avg chars per token = 3.5) * (average recommended chunk size = 375) = 1312.5
            var ragDimensions = 3072;
            if (string.IsNullOrEmpty(index.EmbeddingModel)) index.EmbeddingModel = DefaultEmbeddingModel;
            if (index.EmbeddingModel?.ToLower() != DefaultEmbeddingModel.ToLower()) ragDimensions = 1536; // only text-embedding-3-large supports 3072 dimensions

            var skillsetName = index.Name.ToLower() + "-skillset";
            var skills = new List<SearchIndexerSkill>();
            var projectionInputMappings = new List<InputFieldMappingEntry>();

            // Add default skills and associated projections
            projectionInputMappings.Add(new InputFieldMappingEntry("chunk")
            {
                Source = "/document/pages/*"
            });
            projectionInputMappings.Add(new InputFieldMappingEntry("title")
            {
                Source = "/document/title"
            });
            projectionInputMappings.Add(new InputFieldMappingEntry("topic")
            {
                Source = "/document/topic"
            });
            projectionInputMappings.Add(new InputFieldMappingEntry("keywords")
            {
                Source = "/document/keywords"
            });
            projectionInputMappings.Add(new InputFieldMappingEntry("source")
            {
                Source = "/document/source"
            });
            projectionInputMappings.Add(new InputFieldMappingEntry("created")
            {
                Source = "/document/created"
            });
            projectionInputMappings.Add(new InputFieldMappingEntry("modified")
            {
                Source = "/document/modified"
            });

            skills.Add(
                new SplitSkill(
                    new List<InputFieldMappingEntry> { new InputFieldMappingEntry("text") { Source = "/document/Content" } },
                    new List<OutputFieldMappingEntry> { new OutputFieldMappingEntry("textItems") { TargetName = "pages" } }
                )
                {
                    Context = "/document",
                    TextSplitMode = TextSplitMode.Pages,
                    MaximumPageLength = chunkingLengthInChars,
                    PageOverlapLength = (int)(chunkingLengthInChars * index.ChunkOverlap ?? .1),
                }
            );

            // Add conditional vector fields
            if (index.GenerateContentVector ?? false)
            {
                skills.Add(
                    new AzureOpenAIEmbeddingSkill(
                        new List<InputFieldMappingEntry> { new InputFieldMappingEntry("text") { Source = "/document/pages/*" } },
                        new List<OutputFieldMappingEntry> { new OutputFieldMappingEntry("embedding") { TargetName = "contentVector" } }
                    )
                    {
                        Context = "/document/pages/*",
                        ResourceUri = new Uri(_openaiUrl),
                        ApiKey = _openaiKey,
                        ModelName = index.EmbeddingModel,
                        DeploymentName = index.EmbeddingModel
                    }
                );

                projectionInputMappings.Add(new InputFieldMappingEntry("contentVector") { Source = "/document/pages/*/contentVector" });
            }

            if (index.GenerateTitleVector ?? false)
            {
                skills.Add(
                    new AzureOpenAIEmbeddingSkill(
                        new List<InputFieldMappingEntry> { new InputFieldMappingEntry("text") { Source = "/document/title" } },
                        new List<OutputFieldMappingEntry> { new OutputFieldMappingEntry("embedding") { TargetName = "titleVector" } }
                    )
                    {
                        Context = "/document/pages/*",
                        ResourceUri = new Uri(_openaiUrl),
                        ApiKey = _openaiKey,
                        ModelName = index.EmbeddingModel,
                        DeploymentName = index.EmbeddingModel
                    }
                );

                projectionInputMappings.Add(new InputFieldMappingEntry("titleVector") { Source = "/document/pages/*/titleVector" });
            }

            if (index.GenerateTopicVector ?? false)
            {
                skills.Add(
                    new AzureOpenAIEmbeddingSkill(
                        new List<InputFieldMappingEntry> { new InputFieldMappingEntry("text") { Source = "/document/topic" } },
                        new List<OutputFieldMappingEntry> { new OutputFieldMappingEntry("embedding") { TargetName = "topicVector" } }
                    )
                    {
                        Context = "/document/pages/*",
                        ResourceUri = new Uri(_openaiUrl),
                        ApiKey = _openaiKey,
                        ModelName = index.EmbeddingModel,
                        DeploymentName = index.EmbeddingModel
                    }
                );

                projectionInputMappings.Add(new InputFieldMappingEntry("topicVector") { Source = "/document/pages/*/topicVector" });
            }

            if (index.GenerateKeywordVector ?? false)
            {
                skills.Add(
                    new AzureOpenAIEmbeddingSkill(
                        new List<InputFieldMappingEntry> { new InputFieldMappingEntry("text") { Source = "/document/keywords" } },
                        new List<OutputFieldMappingEntry> { new OutputFieldMappingEntry("embedding") { TargetName = "keywordsVector" } }
                    )
                    {
                        Context = "/document/pages/*",
                        ResourceUri = new Uri(_openaiUrl),
                        ApiKey = _openaiKey,
                        ModelName = index.EmbeddingModel,
                        DeploymentName = index.EmbeddingModel
                    }
                );

                projectionInputMappings.Add(new InputFieldMappingEntry("keywordsVector") { Source = "/document/pages/*/keywordsVector" });
            }

            var skillset = new SearchIndexerSkillset(skillsetName, skills)
            {
                IndexProjection = new SearchIndexerIndexProjection(new[]
                {
                    new SearchIndexerIndexProjectionSelector(index.Name, parentKeyFieldName: "parent_id", sourceContext: "/document/pages/*", mappings: projectionInputMappings)
                })
                {
                    Parameters = new SearchIndexerIndexProjectionsParameters
                    {
                        ProjectionMode = IndexProjectionMode.SkipIndexingParentDocuments
                    }
                }
            };
            var result = await _indexerClient.CreateOrUpdateSkillsetAsync(skillset);
            return skillsetName;
        }

        private SearchIndex GetIndexDefinition(IndexMetadata index)
        {
            // Choose the appropriate rag dimensions for the given embedding model
            var ragDimensions = _defaultRagDimensions;
            if (string.IsNullOrEmpty(index.EmbeddingModel)) index.EmbeddingModel = DefaultEmbeddingModel;
            if (index.EmbeddingModel?.ToLower() != DefaultEmbeddingModel.ToLower()) ragDimensions = 1536; // only text-embedding-3-large supports 3072 dimensions

            var searchIndex = new SearchIndex(index.Name)
            {
                DefaultScoringProfile = index.ScoringProfile?.Name,
                VectorSearch = new()
                {
                    Profiles =
                    {
                        new VectorSearchProfile(DefaultVectorSearchProfile, DefaultVectorAlgConfig)
                        {
                            VectorizerName = DefaultVectorizer,
                        },
                        new VectorSearchProfile(DefaultKnnSearchProfile, DefaultKnnConfig)
                    },
                    Algorithms =
                    {
                        new HnswAlgorithmConfiguration(DefaultVectorAlgConfig),
                        new ExhaustiveKnnAlgorithmConfiguration(DefaultKnnConfig)
                    },
                    Vectorizers =
                    {
                        new AzureOpenAIVectorizer(DefaultVectorizer)
                        {
                            Parameters = new AzureOpenAIVectorizerParameters()
                            {
                                ResourceUri = new Uri(_openaiUrl),
                                ApiKey = _openaiKey,
                                ModelName = index.EmbeddingModel,
                                DeploymentName = index.EmbeddingModel
                            }
                        }
                    }
                },
                SemanticSearch = new()
                {
                    DefaultConfigurationName = SearchQueryType.Semantic.ToString(),
                    Configurations =
                    {
                        new SemanticConfiguration(SearchQueryType.Semantic.ToString(), new()
                        {
                            TitleField = new SemanticField(fieldName: "title"),
                            ContentFields =
                            {
                                new SemanticField(fieldName: "chunk"),
                                new SemanticField(fieldName: "keywords"),
                                new SemanticField(fieldName: "topic")
                            },
                        })
                    },
                },
                Fields =
                {
                    new SearchableField("parent_id") { IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SearchableField("chunk_id") { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true, AnalyzerName = LexicalAnalyzerName.Keyword },
                    new SearchableField("title"),
                    new SearchableField("chunk"),
                    new SearchableField("topic"),
                    new SearchableField("keywords"),
                    new SimpleField("source", SearchFieldDataType.String),
                    new SimpleField("created", SearchFieldDataType.DateTimeOffset),
                    new SimpleField("modified", SearchFieldDataType.DateTimeOffset),
                    new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = ragDimensions,
                        VectorSearchProfileName = DefaultVectorSearchProfile
                    },
                    new SearchField("titleVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = ragDimensions,
                        VectorSearchProfileName = DefaultVectorSearchProfile
                    },
                    new SearchField("topicVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = ragDimensions,
                        VectorSearchProfileName = DefaultVectorSearchProfile
                    },
                    new SearchField("keywordsVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = ragDimensions,
                        VectorSearchProfileName = DefaultVectorSearchProfile
                    },
                },
            };

            return searchIndex;
        }
    }
}
