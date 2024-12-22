using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Implementations
{
    public class RagLogic : IRagLogic
    {
        private readonly IAISearchServiceClient _searchClient;
        private readonly IAGIClient _aiClient;
        private readonly IIndexMetaRepository _metaRepository;
        private readonly IIndexRepository _ragRepository;
        private readonly IValidationHandler _validationHandler;

        public RagLogic(IAGIClient agiClient, IAISearchServiceClient aISearchServiceClient, IIndexMetaRepository metaRepository, IIndexRepository indexRepository, IValidationHandler validationHandler)
        {
            _searchClient = aISearchServiceClient;
            _aiClient = agiClient;
            _metaRepository = metaRepository;
            _ragRepository = indexRepository;
            _validationHandler = validationHandler;
        }

        public async Task<IndexMetadata?> GetRagIndex(string index)
        {
            var dbIndexData = await _metaRepository.GetByNameAsync(index);
            if (dbIndexData == null) return null;
            return DbMappingHandler.MapFromDbIndexMetadata(dbIndexData);
        }

        public async Task<IEnumerable<IndexMetadata>> GetAllIndexesAsync()
        {
            var allIndexes = new List<IndexMetadata>();
            var allDbIndexes = await _metaRepository.GetAllAsync();
            foreach (var dbIndex in allDbIndexes) allIndexes.Add(DbMappingHandler.MapFromDbIndexMetadata(dbIndex));
            return allIndexes;
        }

        public async Task<bool> CreateIndex(IndexMetadata indexDefinition)
        {
            var errorMessage = _validationHandler.ValidateIndexDefinition(indexDefinition);
            if (!string.IsNullOrEmpty(errorMessage)) return false;

            var existing = await _metaRepository.GetByNameAsync(indexDefinition.Name);
            if (existing != null) return false;

            // add index entry for metadata
            var newDbIndex = DbMappingHandler.MapToDbIndexMetadata(indexDefinition);
            var response = await _metaRepository.AddAsync(newDbIndex);
            if (response == null) return false;

            // create a new table for the index
            var success = await _ragRepository.CreateIndexAsync(indexDefinition.Name);
            if (!success) return false;

            success = await _ragRepository.EnableChangeTrackingAsync(indexDefinition.Name);
            if (!success) return false;

            // create the index in Azure AI Search
            success = await _searchClient.UpsertIndex(indexDefinition);
            if (!success) return false;

            // Create a datasource for the SQL DB in Azure AI Search
            success = await _searchClient.CreateDatasource(indexDefinition.Name);
            if (!success) return false;

            // create the indexer to run scheduled ingestions of the datasource
            return await _searchClient.UpsertIndexer(indexDefinition);
        }

        public async Task<bool> ConfigureIndex(IndexMetadata indexDefinition)
        {
            if (string.IsNullOrEmpty(_validationHandler.ValidateIndexDefinition(indexDefinition))) return false;
            var existingDefinition = await _metaRepository.GetByNameAsync(indexDefinition.Name);
            if (existingDefinition == null) return false;

            var success = await _searchClient.UpsertIndex(indexDefinition);
            if (!success) return false;

            success = await _searchClient.UpsertIndexer(indexDefinition);
            if (!success) return false;

            var newDefinition = DbMappingHandler.MapToDbIndexMetadata(indexDefinition);
            var rows = await _metaRepository.UpdateAsync(existingDefinition, newDefinition);
            if (rows < 1) return false;

            // NOTE: Given the below code, disabling generative columns will not destroy existing data

            // Create missing generative data
            if (!existingDefinition.GenerateKeywords && indexDefinition.GenerateKeywords) await GenerateMissingRagField(indexDefinition.Name);
            if (!existingDefinition.GenerateTopic && indexDefinition.GenerateTopic) await GenerateMissingRagField(indexDefinition.Name);
            
            var updateAllDocs = false;
            if (!existingDefinition.GenerateContentVector && indexDefinition.GenerateContentVector) updateAllDocs = true;
            if (!existingDefinition.GenerateTopicVector && indexDefinition.GenerateTopicVector) updateAllDocs = true;
            if (!existingDefinition.GenerateKeywordVector && indexDefinition.GenerateKeywordVector) updateAllDocs = true;
            if (!existingDefinition.GenerateTitleVector && indexDefinition.GenerateTitleVector) updateAllDocs = true;

            if (updateAllDocs) await _ragRepository.MarkIndexForUpdate(indexDefinition.Name);
            return await _searchClient.RunIndexer(indexDefinition.Name);
        }

        private async Task GenerateMissingRagField(string index)
        {
            var allDocs = await _ragRepository.GetAllAsync(index, 1000, 1);

            // perform an update in a way that won't nuke performance here
            foreach ()
        }

        public async Task<bool> RunIndexUpdate(string index)
        {
            if (!_validationHandler.IsValidIndexName(index)) return false;
            var indexMetadata = await _metaRepository.GetByNameAsync(index);
            if (indexMetadata == null) return false;
            return await _searchClient.RunIndexer(index);
        }

        public async Task<bool> DeleteIndex(string index)
        {
            if (!_validationHandler.IsValidIndexName(index)) return false;
            var indexMetadata = await _metaRepository.GetByNameAsync(index);
            if (indexMetadata == null) return false;
            if (await _ragRepository.DeleteIndexAsync(indexMetadata.Name))
            {
                var success = await _searchClient.DeleteIndexer(indexMetadata.Name, indexMetadata.EmbeddingModel ?? GlobalVariables.DefaultEmbeddingModel);
                if (!success) return false;

                success = await _searchClient.DeleteDatasource(indexMetadata.Name);
                if (!success) return false;

                success = await _searchClient.DeleteIndex(index);
                if (!success) return false;

                var rowsAffected = await _metaRepository.DeleteAsync(indexMetadata);
                if (rowsAffected > 0) return true;
            }
            return false;
        }

        public async Task<List<IndexDocument>> QueryIndex(string index, string query)
        {
            throw new NotImplementedException("This API currently recommends performing this operation directly against the AI Search API if required by the client.");
        }

        public async Task<IEnumerable<IndexDocument>?> GetAllDocuments(string index, int count, int page)
        {
            if (!_validationHandler.IsValidIndexName(index)) return null;
            var docList = new List<IndexDocument>();
            var dbDocumentList = await _ragRepository.GetAllAsync(index, count, page);
            foreach (var dbDocument in dbDocumentList) docList.Add(DbMappingHandler.MapFromDbIndexDocument(dbDocument));
            return docList;
        }

        public async Task<IndexDocument?> GetDocument(string index, string document)
        {
            if (!_validationHandler.IsValidIndexName(index)) return null;
            var dbDocument = await _ragRepository.GetDocumentAsync(index, document);
            if (dbDocument == null) return null;
            return DbMappingHandler.MapFromDbIndexDocument(dbDocument);
        }

        public async Task<bool> UpsertDocuments(string index, RagUpsertRequest documentUpsertRequest)
        {
            if (!_validationHandler.IsValidIndexName(index)) return false;

            var indexData = await _metaRepository.GetByNameAsync(index);
            if (indexData == null) return false;

            foreach (var document in documentUpsertRequest.Documents)
            {
                var newDbDocument = DbMappingHandler.MapToDbIndexDocument(document);

                if (indexData.GenerateTopic) document.Topic = await GenerateDocumentMetadata("a topic", document);
                if (indexData.GenerateKeywords) document.Keywords = await GenerateDocumentMetadata("a comma seperated list of keywords", document);

                var existingDoc = await _ragRepository.GetDocumentAsync(index, document.Title);
                if (existingDoc != null)
                {
                    newDbDocument.Modified = DateTimeOffset.UtcNow;
                    var rows = await _ragRepository.UpdateAsync(existingDoc, newDbDocument, indexData.Name);
                    if (rows < 1) return false;
                }
                else
                {
                    newDbDocument.Created = DateTimeOffset.UtcNow;
                    newDbDocument.Modified = DateTimeOffset.UtcNow;
                    var newDoc = await _ragRepository.AddAsync(newDbDocument, indexData.Name);
                    if (newDoc == null) return false;
                }
            }
            return true;
        }

        public async Task<int> DeleteDocuments(string index, string[] documentList)
        {
            var deletedDocuments = 0;
            if (!_validationHandler.IsValidIndexName(index)) return -1;
            foreach (var documentName in documentList)
            {
                var document = await _ragRepository.GetDocumentAsync(index, documentName);
                if (document == null) continue;

                deletedDocuments += await _ragRepository.DeleteAsync(document, index);
            }
            return deletedDocuments;
        }

        #region Private Methods

        private async Task<string> GenerateDocumentMetadata(string dataFormat, IndexDocument document)
        {
            var completion = $"Please create {dataFormat} summarizing the below data delimited by triple " +
                $"backticks. Your response should only contain {dataFormat} and absolutely no other textual " +
                $"data.\n\n";

            // triple backticks to delimit the data
            completion += $"\n```";
            completion += $"\ntitle: {document.Title}";
            completion += $"\ncontent: {document.Content}";
            completion += $"\n```";

            var completionRequest = new CompletionRequest()
            {
                ProfileOptions = new Profile() { Name = GlobalVariables.DefaultAGIModel, System_Message = GlobalVariables.RagRequestSystemMessage, Model = DefaultAGIModel },
                Messages = new List<Message>() { new Message() { Role = GlobalVariables.Role.User, Content = completion } }
            };

            var response = await _aiClient.PostCompletion(completionRequest); // create a seperate method for internal API completions
            return response?.Messages.Last(m => m.Role == GlobalVariables.Role.Assistant).Content ?? string.Empty;
        }

        #endregion
    }
}
