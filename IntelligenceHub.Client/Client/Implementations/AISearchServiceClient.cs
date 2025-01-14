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
            contentEmbedding,
            keywordVector,
            keywordEmbedding,
            topicVector,
            topicEmbedding,
            titleVector,
            titleEmbedding,
            textItems,
            pages,
            chunkSplits,
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

        public async Task<bool> UpsertIndex(IndexMetadata indexDefinition)
        {
            var vectorSize = 3072;
            if (indexDefinition.EmbeddingModel != DefaultEmbeddingModel) vectorSize = 1536;

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
            searchIndex.VectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration(DefaultVectorAlgConfig));
            searchIndex.VectorSearch.Profiles.Add(new VectorSearchProfile(DefaultVectorSearchProfile, DefaultVectorAlgConfig));

            if (indexDefinition.ScoringProfile != null)
            {
                var scoringProfile = new ScoringProfile(indexDefinition.ScoringProfile?.Name ?? DefaultVectorSearchProfile);
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
                if (indexDefinition.ScoringProfile?.FreshnessBoost > 1) scoringProfile.Functions.Add(new FreshnessScoringFunction(RagField.modified.ToString(), indexDefinition.ScoringProfile.FreshnessBoost, new FreshnessScoringParameters(TimeSpan.FromDays(indexDefinition.ScoringProfile.BoostDurationDays)))
                {
                    Interpolation = interpolation,
                });
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

        public async Task<bool> UpsertIndexer(IndexMetadata index)
        {
            // Define the indexer
            SearchIndexer indexer;
            if (index.IndexingInterval is TimeSpan interval) indexer = new SearchIndexer($"{index.Name}-indexer", index.Name, index.Name) { IsDisabled = false, Schedule = new IndexingSchedule(interval), };
            else indexer = new SearchIndexer($"{index.Name}-indexer", index.Name, index.Name) { IsDisabled = true }; // Set IsDasabled true to prevent indexer from immediately running

            

            var response = await _indexerClient.CreateOrUpdateIndexerAsync(indexer);
            if (response.GetRawResponse().IsError) return false;

            // map vector fields
            //indexer.OutputFieldMappings.Add(new FieldMapping("/document/content") { TargetFieldName = RagField.chunk.ToString() });
            //if (index.GenerateTitleVector) indexer.FieldMappings.Add(new FieldMapping("document/titleVector/*") { TargetFieldName = RagField.titleVector.ToString() });
            //if (index.GenerateContentVector) indexer.FieldMappings.Add(new FieldMapping("document/contentVector/*") { TargetFieldName = RagField.contentVector.ToString() });
            //if (index.GenerateTopicVector) indexer.FieldMappings.Add(new FieldMapping("document/topicVector/*") { TargetFieldName = RagField.topicVector.ToString() });
            //if (index.GenerateKeywordVector) indexer.FieldMappings.Add(new FieldMapping("document/keywordVector/*") { TargetFieldName = RagField.keywordVector.ToString() });
            //if (index.GenerateTitleVector) indexer.OutputFieldMappings.Add(new FieldMapping("/document/title") { TargetFieldName = RagField.titleVector.ToString() });
            //if (index.GenerateContentVector) indexer.OutputFieldMappings.Add(new FieldMapping("/document/content") { TargetFieldName = RagField.contentVector.ToString() });
            //if (index.GenerateTopicVector) indexer.OutputFieldMappings.Add(new FieldMapping("/document/topic") { TargetFieldName = RagField.topicVector.ToString() });
            //if (index.GenerateKeywordVector) indexer.OutputFieldMappings.Add(new FieldMapping("/document/keywords") { TargetFieldName = RagField.keywordVector.ToString() });

            indexer.SkillsetName = await UpsertSkillset(index);
            await _indexerClient.CreateOrUpdateIndexerAsync(indexer);

            return true;
        }

        public async Task<bool> RunIndexer(string indexName)
        {
            var response = await _indexerClient.RunIndexerAsync(indexName + "-indexer");
            return response.Status > 199 && response.Status < 300;
        }

        private async Task<string> UpsertSkillset(IndexMetadata index)
        {
            var chunkingLengthInChars = 1312; // (avg chars per token = 3.5) * (average recomended chunk size = 375) = 1312.5
            var ragDimensions = 3072;
            if (index.EmbeddingModel?.ToLower() != DefaultEmbeddingModel.ToLower()) ragDimensions = 1536; // only text-embedding-3-large supports 3072 dimensions

            var skillsetName = index.Name.ToLower() + "-skillset";
            var skills = new List<SearchIndexerSkill>();

            skills.Add(new SplitSkill(
                    new List<InputFieldMappingEntry> { new InputFieldMappingEntry(RagFieldType.text.ToString()) { Source = "/document/content" } },
                    new List<OutputFieldMappingEntry> { new OutputFieldMappingEntry(RagFieldType.textItems.ToString()) { TargetName = RagField.chunkSplits.ToString() } })
                    {
                        Context = "/document",
                        TextSplitMode = TextSplitMode.Pages,
                        MaximumPageLength = chunkingLengthInChars,
                        PageOverlapLength = (int)(chunkingLengthInChars * index.ChunkOverlap),
                    }
                );

            skills.Add(new AzureOpenAIEmbeddingSkill(
                new List<InputFieldMappingEntry>() { new InputFieldMappingEntry(RagFieldType.text.ToString()) { Source = "/document/chunkSplits/*" } },
                new List<OutputFieldMappingEntry>() { new OutputFieldMappingEntry(RagFieldType.embedding.ToString()) { TargetName = RagField.contentVector.ToString() } })
                    {
                        Context = "/document/chunkSplits/*",
                        ResourceUri = new Uri(_openaiUrl),
                        ApiKey = _openaiKey,
                        ModelName = index.EmbeddingModel,
                        Dimensions = ragDimensions,
                        DeploymentName = index.EmbeddingModel
                    }
                );

            //if (index.GenerateTitleVector)
            //{
            //    skills.Add(new AzureOpenAIEmbeddingSkill(
            //        new List<InputFieldMappingEntry>() { new InputFieldMappingEntry(RagFieldType.text.ToString()) { Source = "/document/title" } },
            //        new List<OutputFieldMappingEntry>() { new OutputFieldMappingEntry(RagFieldType.embedding.ToString()) { TargetName = RagField.titleVector.ToString() } })
            //        {
            //            Context = "/document/titleVector",
            //            ResourceUri = new Uri(_openaiUrl),
            //            ApiKey = _openaiKey,
            //            ModelName = index.EmbeddingModel,
            //            Dimensions = ragDimensions,
            //            DeploymentName = index.EmbeddingModel
            //        }
            //    );
            //}

            ////if (index.GenerateContentVector)
            ////{
            ////    skills.Add(new AzureOpenAIEmbeddingSkill(
            ////        new List<InputFieldMappingEntry>() { new InputFieldMappingEntry(RagFieldType.text.ToString()) { Source = "/document/contentVector" } },
            ////        new List<OutputFieldMappingEntry>() { new OutputFieldMappingEntry(RagFieldType.embedding.ToString()) { TargetName = RagField.contentVector.ToString() } })
            ////        {
            ////            Context = "/document/contentVector",
            ////            ResourceUri = new Uri(_openaiUrl),
            ////            ApiKey = _openaiKey,
            ////            ModelName = index.EmbeddingModel,
            ////            Dimensions = ragDimensions,
            ////            DeploymentName = index.EmbeddingModel,
            ////        }
            ////    );
            ////}

            //if (index.GenerateTopicVector)
            //{
            //    skills.Add(new AzureOpenAIEmbeddingSkill(
            //        new List<InputFieldMappingEntry>() { new InputFieldMappingEntry(RagFieldType.text.ToString()) { Source = "/document/topic" } },
            //        new List<OutputFieldMappingEntry>() { new OutputFieldMappingEntry(RagFieldType.embedding.ToString()) { TargetName = RagField.topicVector.ToString() } })
            //        {
            //            Context = "/document/topicVector",
            //            ResourceUri = new Uri(_openaiUrl),
            //            ApiKey = _openaiKey,
            //            ModelName = index.EmbeddingModel,
            //            Dimensions = ragDimensions,
            //            DeploymentName = index.EmbeddingModel
            //        }
            //    );
            //}

            //if (index.GenerateKeywordVector)
            //{
            //    skills.Add(new AzureOpenAIEmbeddingSkill(
            //        new List<InputFieldMappingEntry>() { new InputFieldMappingEntry(RagFieldType.text.ToString()) { Source = "/document/keywords" } },
            //        new List<OutputFieldMappingEntry>() { new OutputFieldMappingEntry(RagFieldType.embedding.ToString()) { TargetName = RagField.keywordVector.ToString() } })
            //        {
            //            Context = "/document/keywordVector",
            //            ResourceUri = new Uri(_openaiUrl),
            //            ApiKey = _openaiKey,
            //            ModelName = index.EmbeddingModel,
            //            Dimensions = ragDimensions,
            //            DeploymentName = index.EmbeddingModel
            //        }
            //    );
            //}

            ////chunk content
            //if (index.GenerateContentVector)
            //{

            //}

            var selectors = new List<SearchIndexerIndexProjectionSelector>()
            {
                new SearchIndexerIndexProjectionSelector(index.Name, "title", "/document/chunkSplits/*", new[]
                {
                    // convert these to enums
                    new InputFieldMappingEntry("title") { Source = "/document/title" },
                    new InputFieldMappingEntry("content") { Source = "/document/content" },
                    new InputFieldMappingEntry("topic") { Source = "/document/topic" },
                    new InputFieldMappingEntry("keywords") { Source = "/document/keywords" },
                    new InputFieldMappingEntry("source") { Source = "/document/source" },
                    new InputFieldMappingEntry("created") { Source = "/document/created" },
                    new InputFieldMappingEntry("modified") { Source = "/document/modified" },
                    new InputFieldMappingEntry("chunkSplits") { Source = "/document/chunkSplits/*" },
                    new InputFieldMappingEntry("contentVector") { Source = "/document/chunkSplits/*/contentVector" },
                    new InputFieldMappingEntry("titleVector") { Source = "/document/title/titleVector" },
                    new InputFieldMappingEntry("topicVector") { Source = "/document/topic/topicVector" },
                    new InputFieldMappingEntry("keywordVector") { Source = "/document/keywordVector" },
                })
            };

            // Create Skillset
            var skillset = new SearchIndexerSkillset(skillsetName, skills) 
            {
                IndexProjection = new SearchIndexerIndexProjection(selectors)
                {
                    Parameters = new SearchIndexerIndexProjectionsParameters
                    {
                        ProjectionMode = IndexProjectionMode.SkipIndexingParentDocuments
                    }
                }
            };
            await _indexerClient.CreateOrUpdateSkillsetAsync(skillset);
            return skillsetName;
        }
    }
}
