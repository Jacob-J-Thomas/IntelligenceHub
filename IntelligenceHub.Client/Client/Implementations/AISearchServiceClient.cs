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
using Microsoft.Extensions.Options;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Client.Implementations
{
    public class AISearchServiceClient : IAISearchServiceClient
    {
        private readonly int _defaultFreshnessBoostDuration = 365; // move to globalvariables
        private readonly int _defaultFreshnessBoost = 2;

        private readonly SearchIndexClient _indexClient;
        private readonly SearchIndexerClient _indexerClient;

        private readonly string _sqlRagDbConnectionString;
        private readonly string _openaiKey;
        private readonly string _openaiUrl;

        private readonly string _vectorAlgConfig = "hnsw";
        private readonly int _ragDimensions = 3072; // move to globalvariables

        // Rag indexes are created with lower case values
        private enum RagField
        {
            title,
            content,
            topic,
            keywords,
            created,
            modified,
            source,
            chunk,
            contentVector,
            keywordsVector,
            topicVector,
            titleVector,
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
            vectorProfile.Queries.Add(new VectorizableTextQuery(query));

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

            options.HighlightFields.Add(RagField.content.ToString());

            var response = await searchClient.SearchAsync<IndexDefinition>(query, options);
            return response.Value;
        }

        public async Task<bool> CreateIndex(IndexMetadata indexDefinition)
        {
            // build fields based off of the class and its attributes
            var fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(IndexDefinition));

            var searchIndex = new SearchIndex(indexDefinition.Name, searchFields)
            {
                DefaultScoringProfile = indexDefinition.ScoringProfile?.Name,
                SemanticSearch = new SemanticSearch() { DefaultConfigurationName = SearchQueryType.Semantic.ToString(), }
            };

            // build a semantic search profile
            var semanticFields = new SemanticPrioritizedFields();
            semanticFields.TitleField = new SemanticField(RagField.title.ToString());
            semanticFields.ContentFields.Add(new SemanticField(RagField.content.ToString()));
            semanticFields.KeywordsFields.Add(new SemanticField(RagField.keywords.ToString()));
            searchIndex.SemanticSearch.Configurations.Add(new SemanticConfiguration(SearchQueryType.Semantic.ToString(), semanticFields));

            // build a vector profile
            searchIndex.VectorSearch = new VectorSearch();
            searchIndex.VectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration(_vectorAlgConfig));
            searchIndex.VectorSearch.Profiles.Add(new VectorSearchProfile(VectorSearchProfileName, _vectorAlgConfig));
            if (indexDefinition.ScoringProfile != null && !string.IsNullOrEmpty(indexDefinition.ScoringProfile.Name))
            {
                var scoringProfile = new ScoringProfile(indexDefinition.ScoringProfile?.Name ?? VectorSearchProfileName);
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
                var freshnessBoostDuration = TimeSpan.FromDays(indexDefinition.ScoringProfile?.BoostDurationDays ?? _defaultFreshnessBoostDuration);
                var freshnessFunction = new FreshnessScoringFunction("modified", indexDefinition.ScoringProfile?.FreshnessBoost ?? _defaultFreshnessBoostDuration, new FreshnessScoringParameters(freshnessBoostDuration))
                {
                    Interpolation = interpolation,
                };   

                if (indexDefinition.ScoringProfile?.FreshnessBoost > 1) scoringProfile.Functions.Add(freshnessFunction);
                else if (indexDefinition.ScoringProfile?.TagBoost > 1)
                {
                    scoringProfile.Functions.Add(new TagScoringFunction(RagField.title.ToString(), indexDefinition.ScoringProfile.TagBoost, new TagScoringParameters(RagField.keywords.ToString()))
                    {
                        Interpolation = interpolation
                    });
                    scoringProfile.Functions.Add(new TagScoringFunction(RagField.content.ToString(), indexDefinition.ScoringProfile.TagBoost, new TagScoringParameters(RagField.keywords.ToString()))
                    {
                        Interpolation = interpolation
                    });
                }
                searchIndex.ScoringProfiles.Add(scoringProfile);
            }
            var response = await _indexClient.CreateIndexAsync(searchIndex);
            return true;
        }

        public async Task<bool> DeleteIndex(string indexName)
        {
            var response = await _indexClient.DeleteIndexAsync(indexName);
            return response.Status > 199 && response.Status < 300;
        }

        public async Task<bool> DeleteIndexer(string indexName, string embeddingModel)
        {
            var skillsetDeletionResponse = await _indexerClient.DeleteSkillsetAsync(embeddingModel + "-embeddingSkillset");
            if (skillsetDeletionResponse == null || skillsetDeletionResponse.IsError) return false;

            var response = await _indexerClient.DeleteIndexerAsync(indexName + "-indexer");
            return response.Status > 199 && response.Status < 300;
        }

        public async Task<bool> CreateDatasource(string databaseName)
        {
            try
            {
                var connectionString = _sqlRagDbConnectionString;

                var container = new SearchIndexerDataContainer(databaseName);
                var connection = new SearchIndexerDataSourceConnection(databaseName, SearchIndexerDataSourceType.AzureSql, connectionString, container)
                {
                    DataChangeDetectionPolicy = new SqlIntegratedChangeTrackingPolicy(),
                };
                try
                {
                    var response = await _indexerClient.CreateDataSourceConnectionAsync(connection);
                    return true;
                }
                catch (Exception ex)
                {
                    throw;
                }
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

        public async Task<bool> CreateIndexer(IndexMetadata index)
        {
            var ragDimensions = _ragDimensions;
            if (index.EmbeddingModel?.ToLower() != DefaultEmbeddingModel.ToLower()) ragDimensions = 1536;

            var connectionString = _sqlRagDbConnectionString;
            var chunkingLengthInChars = 1312; // (avg chars per token = 3.5) * (average recomended chunk size = 375) = 1312.5

            SearchIndexer indexer;
            if (index.IndexingInterval is TimeSpan interval) 
            {
                indexer = new SearchIndexer($"{index.Name}-indexer", index.Name, index.Name)
                {
                    IsDisabled = false, // indexer will automatically run after creation. Adjust this later if needed
                    Schedule = new IndexingSchedule(interval),

                    // probably won't need one
                    //FieldMappings = new FieldMapping[] {}

                    // maybe add later
                    // Parameters = new IndexingParameters(),
                    // OutputFieldMappings = null,
                    // ETag = null,
                    // SkillsetName = null,
                };
            }
            else
            {
                indexer = new SearchIndexer($"{index.Name}-indexer", index.Name, index.Name)
                {
                    IsDisabled = false, // indexer will automatically run after creation. Adjust this later if needed
                };
            }

            

            if (!string.IsNullOrEmpty(index.EmbeddingModel))
            {
                var skillsetName = index.EmbeddingModel + "-embeddingSkillset";
                var skills = new List<SearchIndexerSkill>();

                if (index.GenerateTitleVector) skills.Add(new AzureOpenAIEmbeddingSkill(
                    new List<InputFieldMappingEntry>()
                    {
                        new InputFieldMappingEntry(RagField.title.ToString())
                        {
                            Source = "/document/title"
                        }
                    },
                    new List<OutputFieldMappingEntry>()
                    {
                        new OutputFieldMappingEntry("titleEmbedding")
                        {
                            TargetName = RagField.titleVector.ToString()
                        },
                    })
                {
                    ResourceUri = new Uri(_openaiUrl),
                    ApiKey = _openaiKey,
                    ModelName = index.EmbeddingModel ?? DefaultEmbeddingModel,
                    Dimensions = ragDimensions,

                    // if running into issues check how using the context property works
                });

                if (index.GenerateTopicVector) skills.Add(new AzureOpenAIEmbeddingSkill(
                    new List<InputFieldMappingEntry>()
                    {
                        new InputFieldMappingEntry(RagField.topic.ToString())
                        {
                            Source = "/document/topic"
                        }
                    },
                    new List<OutputFieldMappingEntry>()
                    {
                        new OutputFieldMappingEntry("topicEmbedding")
                        {
                            TargetName = RagField.topicVector.ToString()
                        },
                    })
                {
                    ResourceUri = new Uri(_openaiUrl),
                    ApiKey = _openaiKey,
                    ModelName = index.EmbeddingModel ?? DefaultEmbeddingModel,
                    Dimensions = ragDimensions,
                });

                if (index.GenerateKeywordVector) skills.Add(new AzureOpenAIEmbeddingSkill(
                    new List<InputFieldMappingEntry>()
                    {
                        new InputFieldMappingEntry(RagField.keywords.ToString())
                        {
                            Source = "/document/keywords"
                        }
                    },
                    new List<OutputFieldMappingEntry>()
                    {
                        new OutputFieldMappingEntry("keywordsEmbedding")
                        {
                            TargetName = RagField.keywordsVector.ToString()
                        },
                    })
                {
                    ResourceUri = new Uri(_openaiUrl),
                    ApiKey = _openaiKey,
                    ModelName = index.EmbeddingModel,
                    Dimensions = ragDimensions,
                });

                //chunk content
                if (index.GenerateContentVector)
                {
                    skills.Add(new SplitSkill(
                    new List<InputFieldMappingEntry>
                    {
                        new InputFieldMappingEntry("text") { Source = "/document/content" }
                    },
                    new List<OutputFieldMappingEntry>
                    {
                        new OutputFieldMappingEntry("textItems") { TargetName = "chunkSplits" }
                    })
                    {
                        Context = "/document/content",
                        TextSplitMode = TextSplitMode.Pages,
                        MaximumPageLength = chunkingLengthInChars,
                        PageOverlapLength = (int)(chunkingLengthInChars * index.ChunkOverlap),
                    });

                    skills.Add(new AzureOpenAIEmbeddingSkill(
                        new List<InputFieldMappingEntry>()
                        {
                        new InputFieldMappingEntry(RagField.content.ToString())
                        {
                            Source = "/document/chunkSplits/*"
                        }
                        },
                        new List<OutputFieldMappingEntry>()
                        {
                        new OutputFieldMappingEntry("contentEmbedding")
                        {
                            TargetName = RagField.contentVector.ToString()
                        },
                        })
                    {
                        ResourceUri = new Uri(_openaiUrl),
                        ApiKey = _openaiKey,
                        ModelName = index.EmbeddingModel,
                        Dimensions = ragDimensions,
                    });
                }

                var skillset = new SearchIndexerSkillset(skillsetName, skills);
                await _indexerClient.CreateOrUpdateSkillsetAsync(skillset);
                indexer.SkillsetName = skillsetName;
            }
            var response = await _indexerClient.CreateIndexerAsync(indexer);
            return true;
        }
    }
}
