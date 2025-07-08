using Azure.Search.Documents.Models;
using IntelligenceHub.API.DTOs.RAG;

namespace IntelligenceHub.Client.Interfaces
{
    /// <summary>
    /// A search service client oriented around RAG construction and consumption.
    /// </summary>
    public interface IAISearchServiceClient
    {
        /// <summary>
        /// Retrieves the metadata for a RAG index.
        /// </summary>
        /// <param name="index">The definition of the RAG index.</param>
        /// <param name="query">The query to search against the RAG index.</param>
        /// <returns>Returns the search results retrieved from the RAG index.</returns>
        Task<SearchResults<IndexDefinition>> SearchIndex(IndexMetadata index, string query);

        /// <summary>
        /// Creates a new RAG index.
        /// </summary>
        /// <param name="indexDefinition">The definition of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        Task<bool> CreateIndex(IndexMetadata indexDefinition);

        /// <summary>
        /// Deletes a RAG index and any associated resources.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        Task<bool> DeleteIndex(string indexName);
    }

    /// <summary>
    /// Interface exposing Azure specific operations.
    /// </summary>
    public interface IAzureAISearchServiceClient : IAISearchServiceClient
    {
        /// <summary>
        /// Updates the data in a RAG index, syncing with the corresponding SQL table.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        Task<bool> RunIndexer(string indexName);
    }
}
