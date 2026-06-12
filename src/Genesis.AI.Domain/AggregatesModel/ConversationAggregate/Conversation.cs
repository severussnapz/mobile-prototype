using Genesis.AI.Core.Domain;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

public class Conversation : Entity, IAggregateRoot
{
    public Guid StageId { get; private set; }
    public ConversationStatus Status { get; private set; }
    public int MessageCount { get; private set; }
    public int CurrentPhase { get; private set; }
    public string PhaseName { get; private set; } = "mode_selection";
    public int TotalPhases { get; private set; }
    public int QuestionsAsked { get; private set; }
    public int? EstimatedTotalQuestions { get; private set; }
    public int RequirementsCaptured { get; private set; }

    /// <summary>
    /// The requirement identifier this conversation is scoped to (e.g. "REQ-001").
    /// Null for non-windowed conversations and stages outside P3-P8.
    /// When per-requirement windowing is active, one conversation is created per
    /// requirement so each has an independent, bounded message window.
    /// </summary>
    public string? RequirementId { get; private set; }

    /// <summary>
    /// The orchestration mode for this conversation.
    /// <see cref="OrchestrationMode.ForwardSweep"/> is the default.
    /// <see cref="OrchestrationMode.CrossCheck"/> is entered explicitly for P6/P7/P8
    /// cross-check conversations via the <c>set_orchestration_mode</c> tool.
    /// </summary>
    public OrchestrationMode OrchestrationMode { get; private set; } = OrchestrationMode.ForwardSweep;

    /// <summary>
    /// When this conversation is a continuation of a previous one (e.g. after hitting the
    /// tool-use limit), this references the ID of the conversation it continues from.
    /// Used by the stream controller to inject a handover context block into the system prompt.
    /// </summary>
    public Guid? ContinuedFromConversationId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ResumedAt { get; private set; }

    private readonly List<Message> _messages = [];
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private readonly List<ParkingLotItem> _parkingLotItems = [];
    public IReadOnlyCollection<ParkingLotItem> ParkingLotItems => _parkingLotItems.AsReadOnly();

    private readonly List<TokenUsageRecord> _tokenUsageRecords = [];
    public IReadOnlyCollection<TokenUsageRecord> TokenUsageRecords => _tokenUsageRecords.AsReadOnly();

    private Conversation() { } // Required for EF Core

    public Conversation(Guid stageId, int totalPhases, TimeProvider timeProvider, string? requirementId = null, Guid? continuedFromConversationId = null)
    {
        Id = Guid.NewGuid();
        StageId = stageId;
        Status = ConversationStatus.Active;
        MessageCount = 0;
        CurrentPhase = 0;
        TotalPhases = totalPhases;
        QuestionsAsked = 0;
        RequirementId = requirementId;
        ContinuedFromConversationId = continuedFromConversationId;
        CreatedAt = timeProvider.GetUtcNow();
    }

    public Message AddMessage(MessageRole role, string content, int? tokenCount, TimeProvider timeProvider, string? userErn = null, string? givenName = null, string? familyName = null, List<MessageImage>? images = null, List<MessageDocument>? documents = null)
    {
        var message = new Message(Id, role, content, tokenCount, timeProvider, userErn, givenName, familyName, images, documents);
        _messages.Add(message);
        MessageCount++;
        if (role == MessageRole.User) QuestionsAsked++;
        return message;
    }

    public void AdvancePhase(string phaseName)
    {
        CurrentPhase++;
        PhaseName = phaseName;
    }

    public void SetPhase(int phase, string phaseName)
    {
        CurrentPhase = phase;
        PhaseName = phaseName;
    }

    public void SetEstimatedQuestions(int estimate)
    {
        EstimatedTotalQuestions = estimate;
    }

    public void UpdateProgress(int questionsAsked, int estimatedTotal, int? requirementsCaptured)
    {
        QuestionsAsked = questionsAsked;
        EstimatedTotalQuestions = estimatedTotal;
        if (requirementsCaptured.HasValue)
            RequirementsCaptured = requirementsCaptured.Value;
    }

    public ParkingLotItem AddParkingLotItem(
        string content,
        ParkingLotPriority priority,
        TimeProvider timeProvider)
    {
        var item = new ParkingLotItem(Id, content, priority, CurrentPhase, timeProvider);
        _parkingLotItems.Add(item);
        return item;
    }

    public TokenUsageRecord RecordTokenUsage(
        int inputTokens,
        int outputTokens,
        int cacheReadInputTokens,
        int cacheWriteInputTokens,
        TimeProvider timeProvider)
    {
        var record = new TokenUsageRecord(Id, inputTokens, outputTokens, cacheReadInputTokens, cacheWriteInputTokens, timeProvider);
        _tokenUsageRecords.Add(record);
        return record;
    }

    /// <summary>
    /// Transitions this conversation to the explicit cross-check orchestration mode.
    /// Valid only for P6/P7/P8 stages after the forward sweep is complete.
    /// The mode switch must be requested via the <c>set_orchestration_mode</c> tool —
    /// it is never inferred from turn counts, requirement counts, or queue state.
    /// Forward sweep conversations must never call this.
    /// </summary>
    public void EnterCrossCheckMode()
    {
        OrchestrationMode = OrchestrationMode.CrossCheck;
    }

    public void Resume(TimeProvider timeProvider)
    {
        ResumedAt = timeProvider.GetUtcNow();
        Status = ConversationStatus.Active;
    }

    public void Complete()
    {
        Status = ConversationStatus.Completed;
    }

    public void Pause()
    {
        Status = ConversationStatus.Paused;
    }
}
