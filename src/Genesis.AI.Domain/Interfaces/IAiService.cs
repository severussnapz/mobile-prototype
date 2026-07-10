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
    /// Sends a conversation to the AI and returns the full response.
    /// Allows overriding maximum output tokens (default 32768).
    /// </summary>
    Task<AiResponse> GenerateResponseAsync(
        AiSystemPrompt systemPrompt,
        IReadOnlyList<AiMessage> messages,
        CancellationToken cancellationToken,
        int maxTokens = 32768);

    /// <summary>
    /// Sends a conversation to the AI and streams the response token by token.
    /// </summary>
    IAsyncEnumerable<string> StreamResponseAsync(
        AiSystemPrompt systemPrompt,
        IReadOnlyList<AiMessage> messages,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends a conversation to the AI and streams the response token by token.
    /// Allows overriding maximum output tokens (default 32768).
    /// </summary>
    IAsyncEnumerable<string> StreamResponseAsync(
        AiSystemPrompt systemPrompt,
        IReadOnlyList<AiMessage> messages,
        CancellationToken cancellationToken,
        int maxTokens = 32768);

    /// <summary>
    /// Streams the AI response with tool use support. Yields text chunks and completed tool calls.
    /// The system prompt is split into a stable foundation part (placed before the Bedrock cache
    /// point) and a mutable part (placed after, not cached).
    /// Allows overriding maximum output tokens (default 32768).
    /// </summary>
    IAsyncEnumerable<AiStreamEvent> StreamWithToolsAsync(
        AiSystemPrompt systemPrompt,
        IReadOnlyList<AiMessage> messages,
        IReadOnlyList<AiToolDefinition> tools,
        CancellationToken cancellationToken,
        int maxTokens = 32768);
}
