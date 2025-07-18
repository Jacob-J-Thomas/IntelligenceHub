namespace IntelligenceHub.Business.Handlers;

/// <summary>
/// Defines the payload used to instruct the Azure Function to perform work.
/// </summary>
public class BackgroundTaskMessage
{
    public required string TaskType { get; set; }
    public required string IndexName { get; set; }
    public DAL.Models.DbIndexDocument? Document { get; set; }
}
