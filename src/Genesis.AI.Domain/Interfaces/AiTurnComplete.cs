namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Signals that the AI has finished its current turn and is waiting for tool results
/// before it can continue. The controller must supply results and call ContinueWithToolResultsAsync.
/// </summary>
public record AiTurnComplete(IReadOnlyList<AiToolCall> PendingToolCalls) : AiStreamEvent;
