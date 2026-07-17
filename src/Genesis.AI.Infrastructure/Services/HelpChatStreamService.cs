using System.Text;
using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

public sealed class HelpChatStreamService : IHelpChatStreamService
{
    private readonly IHelpConversationRepository _helpConversationRepository;
    private readonly IKnowledgeService _knowledgeService;
    private readonly IAiService _aiService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HelpChatStreamService> _logger;

    public HelpChatStreamService(
        IHelpConversationRepository helpConversationRepository,
        IKnowledgeService knowledgeService,
        IAiService aiService,
        TimeProvider timeProvider,
        ILogger<HelpChatStreamService> logger)
    {
        ArgumentNullException.ThrowIfNull(helpConversationRepository);
        ArgumentNullException.ThrowIfNull(knowledgeService);
        ArgumentNullException.ThrowIfNull(aiService);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _helpConversationRepository = helpConversationRepository;
        _knowledgeService = knowledgeService;
        _aiService = aiService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public IAsyncEnumerable<string> StreamAsync<TRequest>(TRequest request, CancellationToken cancellationToken)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestObject = (object)request;

        if (TryReadStringProperty(requestObject, "Message", out var message)
            && TryReadGuidProperty(requestObject, "ProjectId", out var projectId)
            && TryReadGuidProperty(requestObject, "HelpConversationId", out var helpConversationId))
        {
            return StreamAsync(message, projectId, helpConversationId, "anonymous", cancellationToken);
        }

        _logger.LogWarning("Unsupported help stream request type: {RequestType}", requestObject.GetType().FullName);
        return Empty();
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string message,
        Guid? projectId,
        Guid? helpConversationId,
        string userErn,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var conversation = await ResolveConversationAsync(helpConversationId, userErn, projectId, cancellationToken);

        var retrievalQuery = BuildRetrievalQuery(conversation, message);
        _logger.LogInformation(
            "HelpChat retrieval query built: {Query} for conversation {ConversationId}, projectId present: {HasProjectId}",
            retrievalQuery,
            conversation.Id,
            projectId.HasValue);

        var (toolChunks, projectChunks) = await RetrieveKnowledgeAsync(retrievalQuery, projectId, cancellationToken);

        var systemPromptText = BuildSystemPrompt(toolChunks, projectChunks);
        var systemPrompt = new AiSystemPrompt(systemPromptText, string.Empty);
        var messages = BuildAiMessages(conversation, message);

        conversation.AddMessage("user", message, _timeProvider);
        await _helpConversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        var responseBuilder = new StringBuilder();
        await foreach (var chunk in _aiService.StreamResponseAsync(systemPrompt, messages, cancellationToken))
        {
            responseBuilder.Append(chunk);
            yield return chunk;
        }

        conversation.AddMessage("assistant", responseBuilder.ToString(), _timeProvider);
        await _helpConversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<HelpConversation> ResolveConversationAsync(
        Guid? helpConversationId,
        string userErn,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        var conversation = helpConversationId.HasValue
            ? await _helpConversationRepository.GetByIdWithMessagesAsync(helpConversationId.Value, cancellationToken)
            : await _helpConversationRepository.GetMostRecentByUserAndProjectAsync(userErn, projectId, cancellationToken);

        if (conversation is not null)
        {
            return conversation;
        }

        conversation = HelpConversation.Create(projectId, userErn, _timeProvider);
        await _helpConversationRepository.AddAsync(conversation, cancellationToken);
        await _helpConversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    private async Task<(IReadOnlyList<KnowledgeChunk> ToolChunks, IReadOnlyList<KnowledgeChunk> ProjectChunks)> RetrieveKnowledgeAsync(
        string retrievalQuery,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        var toolChunks = await _knowledgeService.QueryAsync(
            retrievalQuery,
            KnowledgeNamespace.GenesisTool,
            null,
            3,
            cancellationToken);
        LogRetrievalResults(_logger, "genesis-tool", toolChunks);

        IReadOnlyList<KnowledgeChunk> projectChunks = Array.Empty<KnowledgeChunk>();
        if (projectId.HasValue)
        {
            projectChunks = await _knowledgeService.QueryAsync(
                retrievalQuery,
                KnowledgeNamespace.ProjectArtefact,
                projectId,
                5,
                cancellationToken);
            LogRetrievalResults(_logger, "project-artefact", projectChunks);
        }

        return (toolChunks, projectChunks);
    }

    private static List<AiMessage> BuildAiMessages(HelpConversation conversation, string message)
    {
        var messages = conversation.Messages
            .OrderBy(messageItem => messageItem.CreatedAt)
            .Select(messageItem => new AiMessage(
                messageItem.Role == "user" ? MessageRole.User : MessageRole.Assistant,
                messageItem.Content))
            .ToList();

        messages.Add(new AiMessage(MessageRole.User, message));
        return messages;
    }

    private static string BuildSystemPrompt(
        IReadOnlyList<KnowledgeChunk> toolChunks,
        IReadOnlyList<KnowledgeChunk> projectChunks)
    {
        var projectContent = projectChunks.Count > 0
            ? string.Join("\n\n", projectChunks.Select(projectChunk => projectChunk.Content))
            : "No project context available.";
        var toolContent = string.Join("\n\n", toolChunks.Select(toolChunk => toolChunk.Content));

        var hasProjectContext = projectChunks.Count > 0;
        var contextInstruction = hasProjectContext
            ? "You have access to this project\'s approved artefacts. When answering questions about this project (its requirements, decisions, hazards, architecture, or status), answer from the Project Context below. Only use Genesis AI Knowledge to answer questions about how the Genesis AI tool itself works."
            : "Answer questions about the Genesis AI pipeline and how it works. Be direct and concise.";

        return $"You are the Genesis AI help assistant. {contextInstruction}\n\n## Project Context\n{projectContent}\n\n## Genesis AI Knowledge\n{toolContent}";
    }

    private static void LogRetrievalResults(ILogger logger, string knowledgeNamespace, IReadOnlyList<KnowledgeChunk> chunks)
    {
        logger.LogInformation(
            "HelpChat retrieval returned {ChunkCount} chunks from namespace {Namespace}",
            chunks.Count,
            knowledgeNamespace);

        foreach (var chunk in chunks)
        {
            logger.LogInformation(
                "HelpChat retrieval chunk from {Namespace}: {SourcePath} (score: {Score})",
                knowledgeNamespace,
                chunk.SourcePath,
                chunk.Score);
        }
    }

    internal static string BuildRetrievalQuery(HelpConversation conversation, string currentMessage)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        var priorUserMessage = conversation.Messages
            .Select((messageItem, index) => new { Message = messageItem, Index = index })
            .Where(item => string.Equals(item.Message.Role, "user", StringComparison.Ordinal))
            .OrderByDescending(item => item.Message.CreatedAt)
            .ThenByDescending(item => item.Index)
            .Select(item => item.Message)
            .FirstOrDefault();

        return priorUserMessage is null
            ? currentMessage
            : $"{priorUserMessage.Content}: {currentMessage}";
    }

    private static bool TryReadStringProperty(object source, string propertyName, out string value)
    {
        var property = source.GetType().GetProperty(propertyName);
        if (property?.GetValue(source) is string stringValue)
        {
            value = stringValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryReadGuidProperty(object source, string propertyName, out Guid? value)
    {
        var property = source.GetType().GetProperty(propertyName);
        if (property is null)
        {
            value = null;
            return false;
        }

        var propertyValue = property.GetValue(source);
        if (propertyValue is null)
        {
            value = null;
            return true;
        }

        if (propertyValue is Guid guidValue)
        {
            value = guidValue;
            return true;
        }

        value = null;
        return false;
    }

    private static async IAsyncEnumerable<string> Empty()
    {
        await Task.CompletedTask;
        yield break;
    }
}
