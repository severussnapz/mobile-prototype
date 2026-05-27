namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Emitted when the AI stream encounters a non-recoverable error (e.g. token limit exceeded, content filtered).
/// </summary>
public record AiStreamError(string Reason, string Message) : AiStreamEvent;
