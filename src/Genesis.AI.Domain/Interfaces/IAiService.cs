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
        string systemPrompt,
        IReadOnlyList<AiMessage> messages,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends a conversation to the AI and streams the response token by token.
    /// </summary>
    IAsyncEnumerable<string> StreamResponseAsync(
        string systemPrompt,
        IReadOnlyList<AiMessage> messages,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams the AI response with tool use support. Yields text chunks and completed tool calls.
    /// </summary>
    IAsyncEnumerable<AiStreamEvent> StreamWithToolsAsync(
        string systemPrompt,
        IReadOnlyList<AiMessage> messages,
        IReadOnlyList<AiToolDefinition> tools,
        CancellationToken cancellationToken);
}
