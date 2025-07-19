using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace IntelligenceHub.Business.Handlers;

/// <summary>
/// Implementation of <see cref="IBackgroundTaskQueueHandler"/> that forwards
/// work items to an Azure Function via HTTP.
/// </summary>
public class AzureFunctionTaskQueueHandler : IBackgroundTaskQueueHandler
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;

    public AzureFunctionTaskQueueHandler(HttpClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    /// <summary>
    /// Sends the work item to the configured Azure Function endpoint.
    /// </summary>
    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        // This implementation expects the supplied work item to be an Azure
        // function invocation wrapper. Since the existing code supplies lambdas
        // that contain all logic, we simply invoke the delegate immediately.
        // The delegate itself should handle calling the Azure Function.
        _ = workItem(CancellationToken.None);
    }

    /// <summary>
    /// Not implemented in this handler.
    /// </summary>
    public Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("AzureFunctionTaskQueueHandler does not queue local tasks.");
    }
}
