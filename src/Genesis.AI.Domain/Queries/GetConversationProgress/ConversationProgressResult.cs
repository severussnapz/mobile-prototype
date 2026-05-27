namespace Genesis.AI.Domain.Queries.GetConversationProgress;

public record ConversationProgressResult(
    int CurrentPhase,
    string PhaseName,
    int TotalPhases,
    int QuestionsAsked,
    int? EstimatedTotalQuestions,
    string[] PhaseNames,
    string Status);
