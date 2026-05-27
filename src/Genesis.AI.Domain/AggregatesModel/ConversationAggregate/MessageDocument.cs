namespace Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

public class MessageDocument
{
    public string Data { get; set; } = null!;
    public string MediaType { get; set; } = null!;
    public string FileName { get; set; } = null!;
}
