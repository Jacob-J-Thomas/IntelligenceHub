using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Business.Factories;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.Functions.Models;
using IntelligenceHub.DAL;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Handlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Linq;
using static IntelligenceHub.Common.GlobalVariables;
using Microsoft.EntityFrameworkCore;


namespace IntelligenceHub.Functions.Functions;

public class DocumentUpdateFunction
{
    private readonly ILogger _logger;
    private readonly IAGIClientFactory _agiClientFactory;
    private readonly IIndexRepository _indexRepository;
    private readonly IIndexMetaRepository _metaRepository;
    private readonly IntelligenceHubDbContext _dbContext;

    public DocumentUpdateFunction(ILoggerFactory loggerFactory, IAGIClientFactory agiClientFactory,
        IIndexRepository indexRepository, IIndexMetaRepository metaRepository,
        IntelligenceHubDbContext dbContext)
    {
        _logger = loggerFactory.CreateLogger<DocumentUpdateFunction>();
        _agiClientFactory = agiClientFactory;
        _indexRepository = indexRepository;
        _metaRepository = metaRepository;
        _dbContext = dbContext;
    }

    [Function("DocumentUpdateFunction")] 
    public async Task Run([QueueTrigger("doc-updates", Connection = "AzureWebJobsStorage")] DocumentUpdateMessage message)
    {
        _logger.LogInformation("Processing document update for {Index}: {Document}", message.Index, message.DocumentId);
        var dbIndex = await _metaRepository.GetByNameAsync(message.Index);
        if (dbIndex == null)
        {
            _logger.LogWarning("Index {Index} not found", message.Index);
            return;
        }
        var index = DbMappingHandler.MapFromDbIndexMetadata(dbIndex);
        var document = await _indexRepository.GetDocumentAsync(message.Index, message.DocumentId.ToString());
        if (document == null)
        {
            _logger.LogWarning("Document {Id} not found", message.DocumentId);
            return;
        }
        await RunBackgroundDocumentUpdate(index, document);
    }

    private async Task RunBackgroundDocumentUpdate(IndexMetadata index, DbIndexDocument document)
    {
        var documentDto = DbMappingHandler.MapFromDbIndexDocument(document);
        if (index.GenerateTopic ?? false && string.IsNullOrEmpty(document.Topic))
            document.Topic = await GenerateDocumentMetadata("a topic", documentDto, index.GenerationHost ?? AGIServiceHost.None);
        if (index.GenerateKeywords ?? false && string.IsNullOrEmpty(document.Keywords))
            document.Keywords = await GenerateDocumentMetadata("a comma separated list of keywords", documentDto, index.GenerationHost ?? AGIServiceHost.None);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var existingDoc = await _indexRepository.GetDocumentAsync(index.Name, document.Title);
            if (existingDoc != null)
            {
                document.Modified = DateTimeOffset.UtcNow;
                await _indexRepository.UpdateAsync(existingDoc.Id, document, index.Name);
            }
            else
            {
                document.Created = DateTimeOffset.UtcNow;
                document.Modified = DateTimeOffset.UtcNow;
                await _indexRepository.AddAsync(document, index.Name);
            }
            await transaction.CommitAsync();
        });
    }

    private async Task<string> GenerateDocumentMetadata(string dataFormat, IndexDocument document, AGIServiceHost host)
    {
        if (host == AGIServiceHost.None) return string.Empty;

        var completion = $"Please create {dataFormat} summarizing the below data delimited by triple backticks. Your response should only contain {dataFormat} and absolutely no other textual data.\n\n";
        completion += "\n```";
        completion += $"\ntitle: {document.Title}";
        completion += $"\ncontent: {document.Content}";
        completion += "\n```";

        var model = DefaultOpenAIModel;

        var completionRequest = new CompletionRequest()
        {
            ProfileOptions = new Profile() { Model = model, ImageHost = AGIServiceHost.None },
            Messages = new List<Message>() { new Message() { Role = Role.User, Content = completion } }
        };

        var aiClient = _agiClientFactory.GetClient(host);
        var response = await aiClient.PostCompletion(completionRequest);
        var content = response?.Messages?.Last(m => m.Role == Role.Assistant).Content ?? string.Empty;
        return content.Length > 255 ? content.Substring(0, 255) : content;
    }
}
