namespace IntelligenceHub.Functions.Models;

public class DocumentUpdateMessage
{
    public string Index { get; set; } = string.Empty;
    public int DocumentId { get; set; }
}
