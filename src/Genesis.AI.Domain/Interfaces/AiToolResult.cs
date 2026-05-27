namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Result of executing a tool call, returned to the AI for multi-turn conversations.
/// </summary>
public record AiToolResult(string ToolUseId, string Content);
