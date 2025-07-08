using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.Search.Documents.Models;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace IntelligenceHub.Client.Implementations
{
    /// <summary>
    /// Simple Weaviate client implementing the IAISearchServiceClient interface.
    /// </summary>
    public class WeaviateSearchServiceClient : IAISearchServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _apiKey;

        public WeaviateSearchServiceClient(IHttpClientFactory factory, IOptionsMonitor<WeaviateSearchServiceClientSettings> settings)
        {
            _httpClient = factory.CreateClient();
            _endpoint = settings.CurrentValue.Endpoint.TrimEnd('/');
            _apiKey = settings.CurrentValue.ApiKey;
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body = null)
        {
            var request = new HttpRequestMessage(method, $"{_endpoint}{path}");
            if (!string.IsNullOrEmpty(_apiKey)) request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return request;
        }

        public async Task<SearchResults<IndexDefinition>> SearchIndex(IndexMetadata index, string query)
        {
            var gql = new
            {
                query = $"{{ Get {{ {index.Name}(nearText: {{ concepts: [\"{query}\"] }}) {{ title chunk topic keywords source created modified }} }} }}"
            };
            var req = CreateRequest(HttpMethod.Post, "/v1/graphql", gql);
            var res = await _httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();
            var j = JObject.Parse(content);
            var resultsToken = j["data"]?["Get"]?[index.Name];
            var results = new List<SearchResult<IndexDefinition>>();
            if (resultsToken != null)
            {
                foreach (var item in resultsToken)
                {
                    var doc = new IndexDefinition
                    {
                        title = item.Value<string>("title"),
                        chunk = item.Value<string>("chunk"),
                        topic = item.Value<string>("topic"),
                        keywords = item.Value<string>("keywords"),
                        source = item.Value<string>("source"),
                        created = item.Value<DateTimeOffset?>("created") ?? DateTimeOffset.MinValue,
                        modified = item.Value<DateTimeOffset?>("modified") ?? DateTimeOffset.MinValue
                    };
                    var searchResult = SearchModelFactory.SearchResult(doc, score: null, highlights: null);
                    results.Add(searchResult);
                }
            }
            return SearchModelFactory.SearchResults(values: results, totalCount: results.Count, facets: null, coverage: null, rawResponse: null);
        }

        public async Task<bool> UpsertIndex(IndexMetadata indexDefinition)
        {
            var schema = new
            {
                @class = indexDefinition.Name,
                vectorizer = "none",
                properties = new[]
                {
                    new { name = "title", dataType = new[]{"text"} },
                    new { name = "chunk", dataType = new[]{"text"} },
                    new { name = "topic", dataType = new[]{"text"} },
                    new { name = "keywords", dataType = new[]{"text"} },
                    new { name = "source", dataType = new[]{"text"} },
                    new { name = "created", dataType = new[]{"date"} },
                    new { name = "modified", dataType = new[]{"date"} }
                }
            };
            var req = CreateRequest(HttpMethod.Post, "/v1/schema", schema);
            var res = await _httpClient.SendAsync(req);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteIndex(string indexName)
        {
            var req = CreateRequest(HttpMethod.Delete, $"/v1/schema/{indexName}");
            var res = await _httpClient.SendAsync(req);
            return res.IsSuccessStatusCode;
        }

        public Task<bool> UpsertIndexer(IndexMetadata index) => Task.FromResult(true);
        public Task<bool> RunIndexer(string indexName) => Task.FromResult(true);
        public Task<bool> DeleteIndexer(string indexName) => Task.FromResult(true);
        public Task<bool> CreateDatasource(string databaseName) => Task.FromResult(true);
        public Task<bool> DeleteDatasource(string indexName) => Task.FromResult(true);
    }
}
