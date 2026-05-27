namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// A chunk of text content from the AI response (stream to client).
/// </summary>
public record AiTextChunk(string Text) : AiStreamEvent;
