using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.Business.Handlers
{
    /// <summary>
    /// A handler for managing background tasks.
    /// </summary>
    public interface IBackgroundTaskQueueHandler
    {
        /// <summary>
        /// Queues a message for the background Azure Function.
        /// </summary>
        /// <param name="message">The background task message.</param>
        Task QueueBackgroundWorkItemAsync(BackgroundTaskMessage message, CancellationToken cancellationToken = default);
    }
}
