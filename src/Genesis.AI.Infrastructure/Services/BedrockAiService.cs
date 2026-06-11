using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime.Documents;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocumentFormatType = Amazon.BedrockRuntime.DocumentFormat;

namespace Genesis.AI.Infrastructure.Services;

public sealed class BedrockAiService : IAiService, IDisposable
{
    private readonly AmazonBedrockRuntimeClient _client;
    private readonly string _modelId;
    private readonly int _thinkingBudget;
    private readonly ILogger<BedrockAiService> _logger;

    public BedrockAiService(IConfiguration configuration, ILogger<BedrockAiService> logger)
    {
        _logger = logger;
        var region = configuration["Bedrock:Region"] ?? "eu-west-2";
        _modelId = configuration["Bedrock:ModelId"] ?? "anthropic.claude-sonnet-4-6";
        _thinkingBudget = int.TryParse(configuration["Bedrock:ThinkingBudget"], out var budget) ? budget : 0;

        _client = new AmazonBedrockRuntimeClient(RegionEndpoint.GetBySystemName(region));

        _logger.LogInformation(
            "BedrockAiService configured: model={ModelId}, region={Region}, thinkingBudget={ThinkingBudget}",
            _modelId, region, _thinkingBudget > 0 ? _thinkingBudget : "disabled");
    }

    public async Task<AiResponse> GenerateResponseAsync(
        AiSystemPrompt systemPrompt,
        IReadOnlyList<AiMessage> messages,
        CancellationToken cancellationToken)
    {
        var request = BuildConverseRequest(systemPrompt, messages);

        _logger.LogInformation("Sending Bedrock request with {MessageCount} messages", messages.Count);

        var response = await _client.ConverseAsync(request, cancellationToken);

        var content = response.Output.Message.Content
            .Where(block => block.Text != null)
            .Select(block => block.Text)
            .FirstOrDefault() ?? string.Empty;

        return new AiResponse(
            content,
            (int)response.Usage.InputTokens,
            (int)response.Usage.OutputTokens);
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        AiSystemPrompt systemPrompt,
        IReadOnlyList<AiMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = BuildConverseStreamRequest(systemPrompt, messages);

        _logger.LogInformation("Starting Bedrock stream with {MessageCount} messages", messages.Count);

        var response = await _client.ConverseStreamAsync(request, cancellationToken);

        foreach (var ev in response.Stream.AsEnumerable())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ev is ContentBlockDeltaEvent { Delta.Text: not null } deltaEvent)
            {
                yield return deltaEvent.Delta.Text;
            }
        }
    }

    public async IAsyncEnumerable<AiStreamEvent> StreamWithToolsAsync(
        AiSystemPrompt systemPrompt,
        IReadOnlyList<AiMessage> messages,
        IReadOnlyList<AiToolDefinition> tools,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = BuildConverseStreamRequest(systemPrompt, messages);

        // Add tool configuration
        request.ToolConfig = new ToolConfiguration
        {
            Tools = tools.Select(toolResult => new Tool
            {
                ToolSpec = new ToolSpecification
                {
                    Name = toolResult.Name,
                    Description = toolResult.Description,
                    InputSchema = new ToolInputSchema
                    {
                        Json = ToDocument(toolResult.InputSchema)
                    }
                }
            }).ToList()
        };

        _logger.LogInformation(
            "Starting Bedrock stream with {MessageCount} messages and {ToolCount} tools",
            messages.Count, tools.Count);

        var response = await _client.ConverseStreamAsync(request, cancellationToken);

        // Track in-progress tool use blocks
        string? currentToolName = null;
        string? currentToolUseId = null;
        var toolInputBuffer = new StringBuilder();
        var completedToolCalls = new List<AiToolCall>();

        foreach (var ev in response.Stream.AsEnumerable())
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (ev)
            {
                case ContentBlockStartEvent startEvent when startEvent.Start?.ToolUse != null:
                    // Starting a new tool use block
                    currentToolName = startEvent.Start.ToolUse.Name;
                    currentToolUseId = startEvent.Start.ToolUse.ToolUseId;
                    toolInputBuffer.Clear();
                    break;

                case ContentBlockDeltaEvent deltaEvent:
                    if (deltaEvent.Delta?.Text != null)
                    {
                        // Text content — yield to caller for streaming to client
                        yield return new AiTextChunk(deltaEvent.Delta.Text);
                    }
                    else if (deltaEvent.Delta?.ToolUse?.Input != null && currentToolName != null)
                    {
                        // Tool use input chunk — accumulate
                        toolInputBuffer.Append(deltaEvent.Delta.ToolUse.Input);
                    }
                    break;

                case ContentBlockStopEvent when currentToolName != null:
                    // Tool use block complete — emit the completed tool call
                    var inputJson = toolInputBuffer.ToString();
                    JsonDocument? parsedInput = null;
                    AiStreamError? parseError = null;

                    try
                    {
                        parsedInput = JsonDocument.Parse(
                            string.IsNullOrWhiteSpace(inputJson) ? "{}" : inputJson);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to parse tool input JSON for {ToolName}: {Input}",
                            currentToolName, inputJson);
                        parseError = new AiStreamError(
                            "tool_parse_failure",
                            $"Failed to parse tool input for '{currentToolName}'. The AI response may have been truncated due to output token limits.");
                    }

                    if (parseError != null)
                    {
                        yield return parseError;
                    }
                    else if (parsedInput != null)
                    {
                        _logger.LogInformation(
                            "Tool call completed: {ToolName} (id: {ToolUseId})",
                            currentToolName, currentToolUseId);

                        var toolCall = new AiToolCall(currentToolName, currentToolUseId!, parsedInput);
                        completedToolCalls.Add(toolCall);
                        yield return toolCall;
                    }

                    currentToolName = null;
                    currentToolUseId = null;
                    toolInputBuffer.Clear();
                    break;

                case MessageStopEvent messageStop:
                    // Check stop reason for abnormal termination
                    var stopReason = messageStop.StopReason?.Value;
                    if (stopReason == StopReason.Max_tokens.Value)
                    {
                        _logger.LogWarning("AI stream stopped due to max_tokens limit");
                        yield return new AiStreamError(
                            "max_tokens",
                            "The AI response was cut short because it exceeded the maximum output token limit. Try simplifying your request or breaking it into smaller steps.");
                    }
                    else if (stopReason == StopReason.Content_filtered.Value)
                    {
                        _logger.LogWarning("AI stream stopped due to content filter");
                        yield return new AiStreamError(
                            "content_filtered",
                            "The AI response was blocked by a content safety filter.");
                    }
                    else if (stopReason == StopReason.Guardrail_intervened.Value)
                    {
                        _logger.LogWarning("AI stream stopped due to guardrail intervention");
                        yield return new AiStreamError(
                            "guardrail_intervened",
                            "The AI response was blocked by a guardrail.");
                    }

                    // Turn is complete — if tools were called, signal the controller
                    if (completedToolCalls.Count > 0)
                    {
                        yield return new AiTurnComplete(completedToolCalls.ToList());
                    }
                    break;

                case ConverseStreamMetadataEvent metadataEvent when metadataEvent.Usage != null:
                    var usage = metadataEvent.Usage;
                    _logger.LogInformation(
                        "Token usage — Input: {InputTokens}, Output: {OutputTokens}, Total: {TotalTokens}, CacheRead: {CacheRead}, CacheWrite: {CacheWrite}",
                        usage.InputTokens, usage.OutputTokens, usage.TotalTokens,
                        usage.CacheReadInputTokens, usage.CacheWriteInputTokens);
                    yield return new AiTokenUsage(
                        usage.InputTokens, usage.OutputTokens, usage.TotalTokens,
                        usage.CacheReadInputTokens, usage.CacheWriteInputTokens);
                    break;
            }
        }
    }

    private ConverseRequest BuildConverseRequest(AiSystemPrompt systemPrompt, IReadOnlyList<AiMessage> messages)
    {
        return new ConverseRequest
        {
            ModelId = _modelId,
            System = BuildSystemBlocks(systemPrompt),
            Messages = messages.Select(msg => new Amazon.BedrockRuntime.Model.Message
            {
                Role = msg.Role == MessageRole.User ? ConversationRole.User : ConversationRole.Assistant,
                Content = [new ContentBlock { Text = msg.Content }]
            }).ToList(),
            InferenceConfig = new InferenceConfiguration
            {
                MaxTokens = 32768,
                Temperature = _thinkingBudget > 0 ? 1.0f : 0.7f
            },
            AdditionalModelRequestFields = BuildThinkingConfig()
        };
    }

    private ConverseStreamRequest BuildConverseStreamRequest(AiSystemPrompt systemPrompt, IReadOnlyList<AiMessage> messages)
    {
        return new ConverseStreamRequest
        {
            ModelId = _modelId,
            System = BuildSystemBlocks(systemPrompt),
            Messages = messages.Select(BuildMessage).ToList(),
            InferenceConfig = new InferenceConfiguration
            {
                MaxTokens = 32768,
                Temperature = _thinkingBudget > 0 ? 1.0f : 0.7f
            },
            AdditionalModelRequestFields = BuildThinkingConfig()
        };
    }

    /// <summary>
    /// Builds Bedrock system content blocks from an <see cref="AiSystemPrompt"/>.
    /// When a mutable part is present the layout is:
    ///   [Text(stablePart)] [CachePoint] [Text(mutablePart)]
    /// This means Bedrock caches the stable foundation and processes only the
    /// mutable part (session state, artefact manifest) on each turn — approximately
    /// 10× cheaper for the cached portion.
    /// When no mutable part exists the layout is the original single-block design:
    ///   [Text(stablePart)] [CachePoint]
    /// </summary>
    private static List<SystemContentBlock> BuildSystemBlocks(AiSystemPrompt systemPrompt)
    {
        if (string.IsNullOrEmpty(systemPrompt.MutablePart))
        {
            // Legacy / flag-off path: single prompt block followed by cache point
            return
            [
                new SystemContentBlock { Text = systemPrompt.StablePart },
                new SystemContentBlock { CachePoint = new CachePointBlock { Type = CachePointType.Default } }
            ];
        }

        // Foundation-split path: stable foundation cached, mutable state always fresh
        return
        [
            new SystemContentBlock { Text = systemPrompt.StablePart },
            new SystemContentBlock { CachePoint = new CachePointBlock { Type = CachePointType.Default } },
            new SystemContentBlock { Text = systemPrompt.MutablePart }
        ];
    }

    private Document BuildThinkingConfig()
    {
        if (_thinkingBudget <= 0)
        {
            return new Document(new Dictionary<string, Document>
            {
                ["thinking"] = new Document(new Dictionary<string, Document>
                {
                    ["type"] = new Document("disabled")
                })
            });
        }

        return new Document(new Dictionary<string, Document>
        {
            ["thinking"] = new Document(new Dictionary<string, Document>
            {
                ["type"] = new Document("enabled"),
                ["budget_tokens"] = new Document(_thinkingBudget)
            })
        });
    }

    private static Amazon.BedrockRuntime.Model.Message BuildMessage(AiMessage msg)
    {
        var content = new List<ContentBlock>();

        if (msg.Role == MessageRole.Assistant && msg.ToolCalls is { Count: > 0 })
        {
            // Assistant message with tool calls: text block (if any) + tool_use blocks
            if (!string.IsNullOrEmpty(msg.Content))
                content.Add(new ContentBlock { Text = msg.Content });

            foreach (var tc in msg.ToolCalls)
            {
                content.Add(new ContentBlock
                {
                    ToolUse = new ToolUseBlock
                    {
                        ToolUseId = tc.ToolUseId,
                        Name = tc.ToolName,
                        Input = ToDocument(tc.Input)
                    }
                });
            }
        }
        else if (msg.Role == MessageRole.User && msg.ToolResults is { Count: > 0 })
        {
            // User message with tool results: tool_result blocks
            foreach (var tr in msg.ToolResults)
            {
                content.Add(new ContentBlock
                {
                    ToolResult = new ToolResultBlock
                    {
                        ToolUseId = tr.ToolUseId,
                        Content = [new ToolResultContentBlock { Text = tr.Content }]
                    }
                });
            }
        }
        else
        {
            // User message with images: image blocks first, then text
            if (msg.Images is { Count: > 0 })
            {
                foreach (var image in msg.Images)
                {
                    content.Add(new ContentBlock
                    {
                        Image = new ImageBlock
                        {
                            Format = ParseImageFormat(image.MediaType),
                            Source = new ImageSource
                            {
                                Bytes = new MemoryStream(Convert.FromBase64String(image.Base64Data))
                            }
                        }
                    });
                }
            }

            // User message with documents: document blocks
            if (msg.Documents is { Count: > 0 })
            {
                foreach (var document in msg.Documents)
                {
                    content.Add(new ContentBlock
                    {
                        Document = new DocumentBlock
                        {
                            Format = ParseDocumentFormat(document.MediaType),
                            Name = SanitiseDocumentName(document.FileName),
                            Source = new DocumentSource
                            {
                                Bytes = new MemoryStream(Convert.FromBase64String(document.Base64Data))
                            }
                        }
                    });
                }
            }

            // Simple text message
            content.Add(new ContentBlock { Text = msg.Content });
        }

        return new Amazon.BedrockRuntime.Model.Message
        {
            Role = msg.Role == MessageRole.User ? ConversationRole.User : ConversationRole.Assistant,
            Content = content
        };
    }

    private static ImageFormat ParseImageFormat(string mediaType)
    {
        return mediaType.ToLowerInvariant() switch
        {
            "image/png" => ImageFormat.Png,
            "image/jpeg" or "image/jpg" => ImageFormat.Jpeg,
            "image/gif" => ImageFormat.Gif,
            "image/webp" => ImageFormat.Webp,
            _ => throw new ArgumentException($"Unsupported image format: {mediaType}. Supported: png, jpeg, gif, webp.")
        };
    }

    private static DocumentFormatType ParseDocumentFormat(string mediaType)
    {
        return mediaType.ToLowerInvariant() switch
        {
            "application/pdf" => DocumentFormatType.Pdf,
            "text/csv" => DocumentFormatType.Csv,
            "application/msword" => DocumentFormatType.Doc,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => DocumentFormatType.Docx,
            "application/vnd.ms-excel" => DocumentFormatType.Xls,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => DocumentFormatType.Xlsx,
            "text/html" => DocumentFormatType.Html,
            "text/plain" => DocumentFormatType.Txt,
            "text/markdown" => DocumentFormatType.Md,
            _ => throw new ArgumentException($"Unsupported document format: {mediaType}. Supported: pdf, csv, doc, docx, xls, xlsx, html, txt, md.")
        };
    }

    /// <summary>
    /// Sanitises a filename for use as a DocumentBlock name.
    /// Only alphanumeric, whitespace, hyphens, parentheses, and square brackets are allowed.
    /// </summary>
    private static string SanitiseDocumentName(string fileName)
    {
        // Remove extension and sanitise to allowed characters
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var sanitised = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"[^\w\s\-\(\)\[\]]", "");
        return string.IsNullOrWhiteSpace(sanitised) ? "document" : sanitised.Trim();
    }

    /// <summary>
    /// Converts a System.Text.Json JsonDocument to an Amazon.Runtime.Documents.Document.
    /// </summary>
    private static Document ToDocument(JsonDocument jsonDoc)
    {
        return ConvertElement(jsonDoc.RootElement);
    }

    private static Document ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => new Document(
                element.EnumerateObject().ToDictionary(prop => prop.Name, prop => ConvertElement(prop.Value))),
            JsonValueKind.Array => new Document(
                element.EnumerateArray().Select(ConvertElement).ToList()),
            JsonValueKind.String => new Document(element.GetString()!),
            JsonValueKind.Number => element.TryGetInt64(out var l)
                ? new Document(l)
                : new Document(element.GetDouble()),
            JsonValueKind.True => new Document(true),
            JsonValueKind.False => new Document(false),
            _ => new Document()
        };
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
