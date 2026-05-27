using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

public record AiMessage(
    MessageRole Role,
    string Content,
    IReadOnlyList<AiToolCall>? ToolCalls = null,
    IReadOnlyList<AiToolResult>? ToolResults = null,
    IReadOnlyList<AiImageContent>? Images = null,
    IReadOnlyList<AiDocumentContent>? Documents = null);
