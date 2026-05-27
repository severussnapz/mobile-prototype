namespace Genesis.AI.Api.Dtos;

public sealed class ConversationProgressResponse
{
    public int CurrentPhase { get; init; }
    public string PhaseName { get; init; } = null!;
    public int TotalPhases { get; init; }
    public int QuestionsAsked { get; init; }
    public int? EstimatedTotalQuestions { get; init; }
    public string[] PhaseNames { get; init; } = [];
    public string Status { get; init; } = null!;
}
