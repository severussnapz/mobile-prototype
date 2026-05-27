using System.Text.Json;

namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Defines a tool the AI can call during response generation.
/// </summary>
public record AiToolDefinition(string Name, string Description, JsonDocument InputSchema);
