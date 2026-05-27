using System.Text.Json;

namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// A completed tool call from the AI (execute server-side, don't stream to client).
/// </summary>
public record AiToolCall(string ToolName, string ToolUseId, JsonDocument Input) : AiStreamEvent;
