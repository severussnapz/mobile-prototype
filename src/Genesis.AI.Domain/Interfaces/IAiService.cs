using System.Text.Json;

namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Abstraction for AI model invocation (Bedrock Claude Sonnet 4.6).
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Sends a conversation to the AI and returns the full response.
    /// </summary>
    Task<AiResponse> GenerateResponseAsync(
        AiSystemPrompt systemPrompt,
        IReadOnlyList<AiMessage> messages,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends a conversation to the AI and streams the response token by token.
    /// </summary>
    IAsyncEnumerable<string> StreamResponseAsync(
        AiSystemPrompt systemPrompt,
        IReadOnlyList<AiMessage> messages,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams the AI response with tool use support. Yields text chunks and completed tool calls.
    /// The system prompt is split into a stable foundation part (placed before the Bedrock cache
    /// point) and a mutable part (placed after, not cached).
    /// </summary>
    IAsyncEnumerable<AiStreamEvent> StreamWithToolsAsync(
        AiSystemPrompt systemPrompt,
        IReadOnlyList<AiMessage> messages,
        IReadOnlyList<AiToolDefinition> tools,
        CancellationToken cancellationToken);
}
