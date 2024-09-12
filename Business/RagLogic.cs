using Azure;
using Nest;
using OpenAI.Chat;
using IntelligenceHub.API;
using IntelligenceHub.API.DTOs.ClientDTOs.AICompletionDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.EmbeddingDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.RagDTOs;
using IntelligenceHub.API.DTOs.ControllerDTOs;
using IntelligenceHub.API.DTOs.DataAccessDTOs;
using IntelligenceHub.API.DTOs.System;
using IntelligenceHub.Client;
using IntelligenceHub.Common;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace IntelligenceHub.Business
{
    public class RagLogic
    {
        private readonly AGIClient _aiClient;
        private readonly EmbeddingClient _embeddingClient;
        private readonly RagMetaRepository _metaRepository;
        private readonly RagRepository _ragRepository;
        private readonly string _defaultEmbeddingModel;
        private readonly string _defaultAGIModel;

        public RagLogic(string sqlConnectionString, string ragSqlConnectionString, string aiEndpoint, string aiKey, string defaultEmbeddingModel, string defaultAGIModel) 
        {
            _aiClient = new(aiEndpoint, aiKey);
            _embeddingClient = new(aiEndpoint, aiKey);
            _metaRepository = new(sqlConnectionString);
            _ragRepository = new(ragSqlConnectionString);
            _defaultEmbeddingModel = defaultEmbeddingModel;
            _defaultAGIModel = defaultAGIModel;
        }

        public async Task<RagIndexMetaDataDTO> GetRagIndex(string index)
        {
            return await _metaRepository.GetByNameAsync(index);
        }

        public async Task<IEnumerable<RagIndexMetaDataDTO>> GetAllIndexesAsync()
        {
            return await _metaRepository.GetAllAsync();
        }

        public async Task<bool> CreateIndex(RagIndexMetaDataDTO indexDefinition)
        {
            var existing = await _metaRepository.GetByNameAsync(indexDefinition.Name);
            if (existing != null) return false;
            var response = await _metaRepository.AddAsync(indexDefinition);
            _ragRepository.SetTable(indexDefinition.Name);
            if (response != null) return await _ragRepository.CreateIndexAsync(indexDefinition);
            return false;
        }

        public async Task<bool> ConfigureIndex(RagIndexMetaDataDTO newDefinition)
        {
            var existingDefinition = await _metaRepository.GetByNameAsync(newDefinition.Name);
            if (existingDefinition == null) return false;
            var response = await _metaRepository.UpdateAsync(existingDefinition, newDefinition);

            // execute index update on current data to add any missing properties (such as vectors etc)

            if (response > 0) return true;
            return false;
        }

        public async Task<bool> DeleteIndex(string index)
        {
            var indexMetadata = await _metaRepository.GetByNameAsync(index);
            if (indexMetadata == null) return false;
            _ragRepository.SetTable(index);
            if (await _ragRepository.DeleteIndexAsync())
            {
                var rowsAffected = await _metaRepository.DeleteAsync(indexMetadata);
                if (rowsAffected > 0) return true;
            }
            return false;
        }

        public async Task<List<RagChunk>> QueryIndex(string index, DirectQueryRequest request)
        {
            // other checks/configs?
            var indexData = await _metaRepository.GetByNameAsync(index);
            var queryEmbeddingData = await _embeddingClient.GetEmbeddings(request);
            if (indexData == null || queryEmbeddingData == null) return null;

            _ragRepository.SetTable(index);
            var binaryEmbedding = queryEmbeddingData.Data[0].Embedding.EncodeToBinary();
            var matches = await _ragRepository.CosineSimilarityQueryAsync(request.QueryTarget, binaryEmbedding, request.DocNum);
            if (indexData.TrackDocumentAccessCount) foreach (var match in matches) _ragRepository.UpdateAccessCountAsync(index, match.Id);
            return matches;
        }

        public async Task<IEnumerable<RagChunk>> GetAllDocuments(string index)
        {
            _ragRepository.SetTable(index);
            return await _ragRepository.GetAllAsync();
        }

        public async Task<IEnumerable<RagChunk>> GetDocument(string index, string document)
        {
            _ragRepository.SetTable(index);
            return await _ragRepository.GetAllChunksAsync(index, document);
        }

        public async Task<bool> UpsertDocuments(string index, List<ChunkedRagDocument> documentList)
        {
            var indexData = await _metaRepository.GetByNameAsync(index);
            if (indexData == null) return false;
            string topic = null;
            string keywords = null;
            _ragRepository.SetTable(index);
            foreach (var document in documentList)
            {
                var chunks = await _ragRepository.GetAllChunksAsync(index, document.Title);
                foreach (var chunk in chunks) await _ragRepository.DeleteAsync(chunk);
                foreach (var chunk in document.Chunks)
                {
                    if (indexData.GenerateTopic) chunk.Topic = await GenerateDocumentMetadata("a topic", chunk);
                    if (indexData.GenerateKeywords) chunk.KeyWords = await GenerateDocumentMetadata("keywords", chunk);
                    EmbeddingRequestBase embeddingRequest = new()
                    {
                        Input = chunk.Content,
                        Model = indexData.VectorModel ?? _defaultEmbeddingModel,
                        Encoding_Format = indexData.EncodingFormat,
                        Dimensions = indexData.Dimensions
                    };

                    // move these if checks to another method
                    if (indexData.GenerateContentVector)
                    {
                        var embedding = await _embeddingClient.GetEmbeddings(embeddingRequest);
                        chunk.ContentVectorNorm = CalculateNorm(embedding.Data[0].Embedding);
                        chunk.ContentVector = embedding.Data[0].Embedding.EncodeToBinary();
                    }
                    if (indexData.GenerateTitleVector)
                    {
                        embeddingRequest.Input = chunk.Title;
                        var embedding = await _embeddingClient.GetEmbeddings(embeddingRequest);
                        chunk.TitleVectorNorm = CalculateNorm(embedding.Data[0].Embedding);
                        chunk.TitleVector = embedding.Data[0].Embedding.EncodeToBinary();
                    }
                    // these topic and keyword null checks shouldn't be needed
                    // after validation is added
                    if (chunk.Topic != null && indexData.GenerateTopicVector)
                    {
                        embeddingRequest.Input = chunk.Topic;
                        var embedding = await _embeddingClient.GetEmbeddings(embeddingRequest);
                        chunk.TopicVectorNorm = CalculateNorm(embedding.Data[0].Embedding);
                        chunk.TopicVector = embedding.Data[0].Embedding.EncodeToBinary();
                    }
                    if (chunk.KeyWords != null && indexData.GenerateKeywordVector)
                    {
                        embeddingRequest.Input = chunk.KeyWords;
                        var embedding = await _embeddingClient.GetEmbeddings(embeddingRequest);
                        chunk.KeywordVectorNorm = CalculateNorm(embedding.Data[0].Embedding);
                        chunk.KeywordVector = embedding.Data[0].Embedding.EncodeToBinary();
                    }
                    await _ragRepository.AddAsync(chunk);
                }
            }
            return true;
        }

        public async Task<List<ChunkedRagDocument>> ChunkDocuments(string index, RagUpsertRequest request)
        {
            var indexData = await _metaRepository.GetByNameAsync(index);

            // Adjust estimated tokens based off of the percentage of overlapping text
            var charsPerToken = 4;
            var remainder = 1;
            var tokenIncreasePercentage = 1 + (indexData.ChunkOverlap / 100.00);
            List<ChunkedRagDocument> chunkedData = new();
            foreach (var document in request.Documents)
            {
                ChunkedRagDocument chunkedDocument = new()
                {
                    Title = document.Title,
                    SourceName = document.SourceName,
                    SourceLink = document.SourceLink,
                    PermissionGroup = document.PermissionGroup,
                    CreatedDate = document.CreatedDate,
                    ModifiedDate = document.ModifiedDate,
                };
                var docLength = document.Content.Length;
                if (docLength > indexData.ChunkLength)
                {
                    var numTokensWithOverlapping = (docLength / charsPerToken) * tokenIncreasePercentage;
                    var numSplits = (int)Math.Ceiling(numTokensWithOverlapping / indexData.ChunkLength + remainder);
                    var splitLength = docLength / numSplits;
                    var overlapLength = (int)(splitLength * indexData.ChunkOverlap / 100.00); // assuming ChunkOverlap is a percentage
                    var chunkIndex = 0;
                    for (var i = 0; i < docLength; i += splitLength - overlapLength)
                    {
                        var length = Math.Min(splitLength, docLength - i);
                        RagChunk chunk = new()
                        {
                            Title = document.Title,
                            Content = document.Content.Substring(i, length),
                            Chunk = chunkIndex,
                            SourceName = document.SourceName,
                            SourceLink = document.SourceLink,
                            PermissionGroup = document.PermissionGroup,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                        };
                        chunkIndex++;
                        chunkedDocument.Chunks.Add(chunk);
                    }
                }
                else
                {
                    RagChunk chunk = new()
                    {
                        Title = document.Title,
                        Content = document.Content,
                        Chunk = 0,
                        SourceName = document.SourceName,
                        SourceLink = document.SourceLink,
                        PermissionGroup = document.PermissionGroup,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                    };
                    chunk.Content = document.Content;
                    chunkedDocument.Chunks.Add(chunk);
                }
                chunkedData.Add(chunkedDocument);
            }
            return chunkedData;
        }

        // handle chunks here?
        public async Task<int> DeleteDocuments(string index, string[] documentList)
        {
            var deletedChunks = 0;
            _ragRepository.SetTable(index);
            foreach (var document in documentList)
            {
                var allChunks = await _ragRepository.GetAllChunksAsync(index, document);// probably make this GetAllChunks or something
                foreach (var chunk in allChunks) deletedChunks += await _ragRepository.DeleteAsync(chunk);
            }
            return deletedChunks;
        }

        private float CalculateNorm(float[] vectors)
        {
            float sumOfSquares = 0;
            foreach (var v in vectors) sumOfSquares += v * v;
            return (float)Math.Sqrt(sumOfSquares);
        }

        private async Task<string> GenerateDocumentMetadata(string dataFormat, RagChunk document)
        {
            var completion = $"Please create {dataFormat} summarizing the below data delimited by triple " +
                $"backticks. Your response should only contain {dataFormat} and absolutely no other textual " +
                $"data.\n\n";

            // triple backticks to delimit the data
            completion += "```";
            completion += $"title: {document.Title}\n\ncontent: {document.Content}";
            completion += "\n```";

            var completionRequest = new CompletionRequest()
            {
                Model = _defaultAGIModel,
                Messages = new List<ChatMessage>()
                { 
                    new SystemChatMessage(GlobalVariables.RagRequestSystemMessage),
                    new UserChatMessage(completion)
                }
            };

            var response = await _aiClient.PostCompletion(completionRequest);
            return response.Content;
        }
    }
}
