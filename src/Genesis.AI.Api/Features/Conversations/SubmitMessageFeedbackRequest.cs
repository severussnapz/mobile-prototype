namespace Genesis.AI.Api.Features.Conversations;

public class SubmitMessageFeedbackRequest
{
    public bool IsHelpful { get; set; }
    public string? Reason { get; set; }
}