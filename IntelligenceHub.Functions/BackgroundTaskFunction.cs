using System.Text.Json;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Business.Factories;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.Client.Implementations;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.Functions;

public class BackgroundTaskFunction
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BackgroundTaskFunction(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    [Function(nameof(BackgroundTaskFunction))]
    public async Task RunAsync([QueueTrigger("background-tasks", Connection = "QueueConnectionString")] string queueItem, FunctionContext context)
    {
        var logger = context.GetLogger<BackgroundTaskFunction>();
        BackgroundTaskMessage? message = null;
        try
        {
            message = JsonSerializer.Deserialize<BackgroundTaskMessage>(queueItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize queue message");
            return;
        }

        if (message == null)
        {
            logger.LogError("Queue message was null");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var ragLogic = new RagLogic(
            services.GetRequiredService<IOptionsMonitor<Settings>>(),
            services.GetRequiredService<IAGIClientFactory>(),
            services.GetRequiredService<IProfileRepository>(),
            services.GetRequiredService<IRagClientFactory>(),
            services.GetRequiredService<IIndexMetaRepository>(),
            services.GetRequiredService<IIndexRepository>(),
            services.GetRequiredService<IValidationHandler>(),
            new NoOpBackgroundTaskQueueHandler(),
            services.GetRequiredService<IntelligenceHubDbContext>());

        switch (message.TaskType)
        {
            case "DocumentUpdate":
                if (message.Document != null)
                {
                    var request = new RagUpsertRequest
                    {
                        Documents = new List<IndexDocument>
                        {
                            DAL.DbMappingHandler.MapFromDbIndexDocument(message.Document!)
                        }
                    };
                    await ragLogic.UpsertDocuments(message.IndexName, request);
                }
                break;
            case "SyncWeaviate":
                await SyncWeaviateIndexAsync(
                    message.IndexName,
                    services.GetRequiredService<IIndexRepository>(),
                    services.GetRequiredService<WeaviateSearchServiceClient>());
                break;
            default:
                logger.LogWarning($"Unknown task type '{message.TaskType}'");
                break;
        }
    }

    private static async Task SyncWeaviateIndexAsync(string index, IIndexRepository repository, WeaviateSearchServiceClient client)
    {
        const int batch = 100;
        int page = 1;
        var sqlDocs = new List<DAL.Models.DbIndexDocument>();
        IEnumerable<DAL.Models.DbIndexDocument> pageDocs;
        do
        {
            pageDocs = await repository.GetAllAsync(index, batch, page);
            sqlDocs.AddRange(pageDocs);
            page++;
        } while (pageDocs.Any());

        var weavDocs = await client.GetAllDocuments(index);
        var sqlLookup = sqlDocs.ToDictionary(d => d.Id);

        foreach (var wdoc in weavDocs)
        {
            if (!sqlLookup.ContainsKey(wdoc.Id))
            {
                await client.DeleteDocument(index, wdoc.Id);
            }
        }

        foreach (var sdoc in sqlDocs)
        {
            var dto = DAL.DbMappingHandler.MapFromDbIndexDocument(sdoc);
            await client.UpsertDocument(index, dto);
        }
    }
}
