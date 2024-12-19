using IntelligenceHub.Client;
using IntelligenceHub.Common;
using IntelligenceHub.DAL;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using System.Text.RegularExpressions;

namespace IntelligenceHub.Business
{
    public class RagLogic
    {
        private readonly IAISearchServiceClient _searchClient;
        private readonly IAGIClient _aiClient;
        private readonly IndexMetaRepository _metaRepository;
        private readonly IndexRepository _ragRepository;
        private readonly string _defaultAGIModel;

        private readonly string _defaultEmbeddingModel = "text-embedding-3-large";

        public RagLogic(IAGIClient agiClient, IAISearchServiceClient aISearchServiceClient, string sqlConnectionString) 
        {
            _searchClient = aISearchServiceClient;
            _aiClient = agiClient;
            _metaRepository = new IndexMetaRepository(sqlConnectionString);
            _ragRepository = new IndexRepository(sqlConnectionString);
        }

        public async Task<IndexMetadata> GetRagIndex(string index)
        {
            var dbIndexData = await _metaRepository.GetByNameAsync(index);
            return DbMappingHandler.MapFromDbIndexMetadata(dbIndexData);
        }

        public async Task<IEnumerable<IndexMetadata>> GetAllIndexesAsync()
        {
            var allIndexes = new List<IndexMetadata>();
            var allDbIndexes = await _metaRepository.GetAllAsync();
            foreach(var dbIndex in allDbIndexes) allIndexes.Add(DbMappingHandler.MapFromDbIndexMetadata(dbIndex));
            return allIndexes;
        }

        public async Task<bool> CreateIndex(IndexMetadata indexDefinition)
        {
            if (!IsValidIndexName(indexDefinition.Name)) return false;
            var existing = await _metaRepository.GetByNameAsync(indexDefinition.Name);
            if (existing != null) return false;

            // add index entry for metadata
            var newDbIndex = DbMappingHandler.MapToDbIndexMetadata(indexDefinition);
            var response = await _metaRepository.AddAsync(newDbIndex);
            if (response == null) return false;

            // create a new table for the index
            var success = await _ragRepository.CreateIndexAsync(indexDefinition.Name);
            if (!success) return false;

            // create the index in Azure AI Search
            success = await _searchClient.CreateIndex(indexDefinition);
            if (!success) return false;

            // Create a datasource for the SQL DB in Azure AI Search
            success = await _searchClient.CreateDatasource(indexDefinition.Name);
            if (!success) return false;

            // create the indexer
            return await _searchClient.CreateIndexer(indexDefinition);
        }

        public async Task<bool> ConfigureIndex(IndexMetadata newDefinition)
        {
            throw new NotImplementedException();

            //var existingDefinition = await _metaRepository.GetByNameAsync(newDefinition.Name);
            //if (existingDefinition == null) return false;

            //var response = await _metaRepository.UpdateAsync(existingDefinition, newDefinition);

            //// execute index update on current data to add any missing properties (such as vectors etc)

            //if (response > 0) return true;
            //return false;
        }

        public async Task<bool> DeleteIndex(string index)
        {
            if (!IsValidIndexName(index)) return false;
            var indexMetadata = await _metaRepository.GetByNameAsync(index);
            if (indexMetadata == null) return false;
            if (await _ragRepository.DeleteIndexAsync(indexMetadata.Name))
            {
                var success = await _searchClient.DeleteIndexer(indexMetadata.Name, indexMetadata.EmbeddingModel ?? _defaultEmbeddingModel);
                if (!success) return false;

                success = await _searchClient.DeleteDatasource(indexMetadata.Name);
                if (!success) return false;

                success = await _searchClient.DeleteIndex(index);
                if (!success) return false;

                var rowsAffected = await _metaRepository.DeleteAsync(indexMetadata, indexMetadata.Name);
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
            if (!IsValidIndexName(index)) return null;
            var docList = new List<IndexDocument>();
            var dbDocumentList = await _ragRepository.GetAllAsync(count, page);
            foreach (var dbDocument in dbDocumentList) docList.Add(DbMappingHandler.MapFromDbIndexDocument(dbDocument));
            return docList;
        }

        public async Task<IndexDocument?> GetDocument(string index, string document)
        {
            if (!IsValidIndexName(index)) return null;
            var dbDocument = await _ragRepository.GetDocumentAsync(index, document);
            if (dbDocument == null) return null;
            return DbMappingHandler.MapFromDbIndexDocument(dbDocument);
        }

        public async Task<bool> UpsertDocuments(string index, RagUpsertRequest documentUpsertRequest)
        {
            if (!IsValidIndexName(index)) return false;

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
            if (!IsValidIndexName(index)) return -1;
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
                ProfileOptions = new Profile() { Name = _defaultAGIModel },
                Messages = new List<Message>()
                { 
                    new Message() { Role = GlobalVariables.Role.System, Content = GlobalVariables.RagRequestSystemMessage },
                    new Message() { Role = GlobalVariables.Role.User, Content = completion }
                }
            };

            var response = await _aiClient.PostCompletion(completionRequest);
            return response?.Messages.Last(m => m.Role == GlobalVariables.Role.Assistant).Content ?? string.Empty;
        }

        private static bool IsValidIndexName(string tableName)
        {
            // Regular expression to match valid table names (alphanumeric characters and underscores only)

            // change this to mitigate possibility of DOS attacks
            string pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";

            // Check if the table name matches the pattern and is not a SQL keyword
            if (Regex.IsMatch(tableName, pattern))
            {
                return !IsSqlKeyword(tableName);
            }

            return false;
        }

        private static bool IsSqlKeyword(string tableName)
        {
            // List of common SQL keywords to prevent
            string[] sqlKeywords = new string[]
            {
                "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TABLE",
                "WHERE", "FROM", "JOIN", "UNION", "ORDER", "GROUP", "HAVING"
            };

            // Check if the table name matches any SQL keyword (case-insensitive)
            foreach (string keyword in sqlKeywords)
            {
                if (string.Equals(tableName, keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
