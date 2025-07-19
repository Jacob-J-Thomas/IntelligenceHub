using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.Functions.Models;
using IntelligenceHub.Business.Handlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using IntelligenceHub.Client.Implementations;
using System.Linq;
using IntelligenceHub.DAL;

namespace IntelligenceHub.Functions.Functions;

public class WeaviateSyncFunction
{
    private readonly ILogger _logger;
    private readonly IIndexRepository _indexRepository;
    private readonly WeaviateSearchServiceClient _weaviateClient;

    public WeaviateSyncFunction(ILoggerFactory loggerFactory, IIndexRepository indexRepository,
        WeaviateSearchServiceClient weaviateClient)
    {
        _logger = loggerFactory.CreateLogger<WeaviateSyncFunction>();
        _indexRepository = indexRepository;
        _weaviateClient = weaviateClient;
    }

    [Function("WeaviateSyncFunction")]
    public async Task Run([QueueTrigger("weav-sync", Connection = "AzureWebJobsStorage")] WeaviateSyncMessage message)
    {
        _logger.LogInformation("Syncing Weaviate index {Index}", message.Index);
        await SyncWeaviateIndex(message.Index);
    }

    private async Task SyncWeaviateIndex(string index)
    {
        const int batch = 100;
        int page = 1;
        var sqlDocs = new List<DbIndexDocument>();
        IEnumerable<DbIndexDocument> pageDocs;
        do
        {
            pageDocs = await _indexRepository.GetAllAsync(index, batch, page);
            sqlDocs.AddRange(pageDocs);
            page++;
        } while (pageDocs.Any());

        var weavDocs = await _weaviateClient.GetAllDocuments(index);
        var sqlLookup = sqlDocs.ToDictionary(d => d.Id);

        foreach (var wdoc in weavDocs)
        {
            if (!sqlLookup.ContainsKey(wdoc.Id))
            {
                await _weaviateClient.DeleteDocument(index, wdoc.Id);
            }
        }

        foreach (var sdoc in sqlDocs)
        {
            var dto = DbMappingHandler.MapFromDbIndexDocument(sdoc);
            await _weaviateClient.UpsertDocument(index, dto);
        }
    }
}
