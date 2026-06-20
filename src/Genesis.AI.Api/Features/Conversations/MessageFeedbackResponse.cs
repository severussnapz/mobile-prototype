namespace Genesis.AI.Api.Features.Conversations;

public class MessageFeedbackResponse
{
    public Guid MessageId { get; set; }
    public string StageType { get; set; } = null!;
    public bool IsHelpful { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}