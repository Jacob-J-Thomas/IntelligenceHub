using IntelligenceHub.Business.Handlers;

namespace IntelligenceHub.Functions;

/// <summary>
/// Minimal implementation of <see cref="IBackgroundTaskQueueHandler"/> used by the Azure Function
/// to satisfy dependency requirements. Work items are ignored because the function
/// executes tasks directly.
/// </summary>
public class NoOpBackgroundTaskQueueHandler : IBackgroundTaskQueueHandler
{
    public Task QueueBackgroundWorkItemAsync(BackgroundTaskMessage message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
