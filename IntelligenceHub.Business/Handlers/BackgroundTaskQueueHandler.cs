using Azure.Storage.Queues;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace IntelligenceHub.Business.Handlers;

/// <summary>
/// Implementation of <see cref="IBackgroundTaskQueueHandler"/> that sends messages
/// to an Azure Storage queue. These messages are processed by an Azure Function.
/// </summary>
public class BackgroundTaskQueueHandler : IBackgroundTaskQueueHandler
{
    private readonly QueueClient _queueClient;

    public BackgroundTaskQueueHandler(IConfiguration configuration)
    {
        var connection = configuration["QueueConnectionString"];
        _queueClient = new QueueClient(connection, "background-tasks");
        _queueClient.CreateIfNotExists();
    }

    /// <inheritdoc/>
    public async Task QueueBackgroundWorkItemAsync(BackgroundTaskMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(message);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        await _queueClient.SendMessageAsync(base64, cancellationToken);
    }
}
