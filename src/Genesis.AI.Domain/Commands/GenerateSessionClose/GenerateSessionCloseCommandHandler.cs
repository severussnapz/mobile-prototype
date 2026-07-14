using System.Text;
using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Domain.Commands.GenerateSessionClose;

public sealed class GenerateSessionCloseCommandHandler
    : IRequestHandler<GenerateSessionCloseCommand, GenerateSessionCloseResult>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _storageService;
    private readonly IAiService _aiService;
    private readonly ISessionCloseSkillBuilder _skillBuilder;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GenerateSessionCloseCommandHandler> _logger;

    public GenerateSessionCloseCommandHandler(
        IConversationRepository conversationRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService storageService,
        IAiService aiService,
        ISessionCloseSkillBuilder skillBuilder,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<GenerateSessionCloseCommandHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _artefactRepository = artefactRepository;
        _storageService = storageService;
        _aiService = aiService;
        _skillBuilder = skillBuilder;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<GenerateSessionCloseResult> Handle(GenerateSessionCloseCommand command, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(command.ConversationId, cancellationToken);
        if (conversation is null)
        {
            throw new NotFoundException($"Conversation {command.ConversationId} not found.");
        }

        var summary = BuildConversationSummary(conversation);

        var prompt = _skillBuilder.Build(command.StageType, summary);

        var aiResponse = await _aiService.GenerateResponseAsync(
            AiSystemPrompt.FromFullPrompt(prompt),
            [new AiMessage(MessageRole.User, "Generate the session close document.")],
            cancellationToken);

        var content = aiResponse.Content;
        var filePath = SessionCloseStageMap.GetFilePath(command.StageType);

        var (artefact, version) = await UpsertSessionCloseArtefactAsync(command, filePath, content, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generated session close artefact {ArtefactId} for project {ProjectId} conversation {ConversationId} at {FilePath} v{Version}.",
            artefact.Id,
            command.ProjectId,
            command.ConversationId,
            filePath,
            version);

        return new GenerateSessionCloseResult(artefact.Id, filePath, version);
    }

    private static string BuildConversationSummary(Genesis.AI.Domain.AggregatesModel.ConversationAggregate.Conversation conversation)
    {
        return string.Join(
            "\n\n",
            conversation.Messages
                .OrderBy(message => message.CreatedAt)
                .TakeLast(20)
                .Select(message => $"{message.Role}: {message.Content}"));
    }

    private async Task<(Artefact Artefact, int Version)> UpsertSessionCloseArtefactAsync(
        GenerateSessionCloseCommand command,
        string filePath,
        string content,
        CancellationToken cancellationToken)
    {
        var existing = await _artefactRepository.GetByProjectAndFilePathAsync(
            command.ProjectId,
            filePath,
            cancellationToken);

        if (existing is null)
        {
            return await CreateArtefactAsync(command, filePath, content, cancellationToken);
        }

        return await UpdateArtefactAsync(command, existing, filePath, content, cancellationToken);
    }

    private async Task<(Artefact Artefact, int Version)> CreateArtefactAsync(
        GenerateSessionCloseCommand command,
        string filePath,
        string content,
        CancellationToken cancellationToken)
    {
        var version = await _artefactRepository.GetNextVersionForFileAsync(command.ProjectId, filePath, cancellationToken);
        var (s3Key, sizeBytes) = await SaveArtefactContentAsync(command.ProjectId, filePath, version, content, cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            command.ProjectId,
            version,
            filePath,
            s3Key,
            "text/markdown",
            sizeBytes,
            command.UserErn,
            _timeProvider,
            true);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
        return (artefact, version);
    }

    private async Task<(Artefact Artefact, int Version)> UpdateArtefactAsync(
        GenerateSessionCloseCommand command,
        Artefact existing,
        string filePath,
        string content,
        CancellationToken cancellationToken)
    {
        var version = existing.Version + 1;
        var (s3Key, sizeBytes) = await SaveArtefactContentAsync(command.ProjectId, filePath, version, content, cancellationToken);

        existing.ReplaceContent(version, s3Key, "text/markdown", sizeBytes, command.UserErn, _timeProvider);
        await _artefactRepository.UpdateAsync(existing, cancellationToken);
        return (existing, version);
    }

    private async Task<(string S3Key, int SizeBytes)> SaveArtefactContentAsync(
        Guid projectId,
        string filePath,
        int version,
        string content,
        CancellationToken cancellationToken)
    {
        var s3Key = await _storageService.SaveContentAsync(
            projectId,
            filePath,
            version,
            content,
            "text/markdown",
            cancellationToken);

        var sizeBytes = Encoding.UTF8.GetByteCount(content);
        return (s3Key, sizeBytes);
    }
}