using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Commands.GenerateSessionClose;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Genesis.AI.Tests.Domain.Commands.GenerateSessionClose;

public sealed class GenerateSessionCloseCommandHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepository;
    private readonly Mock<IArtefactRepository> _artefactRepository;
    private readonly Mock<IArtefactStorageService> _artefactStorageService;
    private readonly Mock<IAiService> _aiService;
    private readonly Mock<ISessionCloseSkillBuilder> _sessionCloseSkillBuilder;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly FakeTimeProvider _timeProvider;
    private readonly Mock<ILogger<GenerateSessionCloseCommandHandler>> _logger;
    private readonly GenerateSessionCloseCommandHandler _handler;

    public GenerateSessionCloseCommandHandlerTests()
    {
        _conversationRepository = new Mock<IConversationRepository>();
        _artefactRepository = new Mock<IArtefactRepository>();
        _artefactStorageService = new Mock<IArtefactStorageService>();
        _aiService = new Mock<IAiService>();
        _sessionCloseSkillBuilder = new Mock<ISessionCloseSkillBuilder>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 07, 07, 9, 0, 0, TimeSpan.Zero));
        _logger = new Mock<ILogger<GenerateSessionCloseCommandHandler>>();

        _conversationRepository
            .Setup(repository => repository.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid conversationId, CancellationToken _) => CreateConversation(conversationId));
        _artefactRepository
            .Setup(repository => repository.GetByProjectAndFilePathAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);
        _artefactRepository
            .Setup(repository => repository.GetNextVersionForFileAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _artefactStorageService
            .Setup(service => service.SaveContentAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("projects/p/session-close/v1");
        _aiService
            .Setup(service => service.GenerateResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse("# Session Close\n\nSummary content", 1, 1, 0, 0));
        _sessionCloseSkillBuilder
            .Setup(builder => builder.Build(It.IsAny<StageType>(), It.IsAny<string>()))
            .Returns("skill prompt");
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new GenerateSessionCloseCommandHandler(
            _conversationRepository.Object,
            _artefactRepository.Object,
            _artefactStorageService.Object,
            _aiService.Object,
            _sessionCloseSkillBuilder.Object,
            _unitOfWork.Object,
            _timeProvider,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ThrowsNotFoundException()
    {
        var command = new GenerateSessionCloseCommand(Guid.NewGuid(), Guid.NewGuid(), StageType.RequirementsDiscovery, "user-1");
        _conversationRepository
            .Setup(repository => repository.GetByIdWithMessagesAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task Handle_NewArtefact_CreatesWithVersionOne()
    {
        var command = new GenerateSessionCloseCommand(Guid.NewGuid(), Guid.NewGuid(), StageType.RequirementsDiscovery, "user-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, result.Version);
        _artefactRepository.Verify(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingArtefact_IncrementsVersion()
    {
        var command = new GenerateSessionCloseCommand(Guid.NewGuid(), Guid.NewGuid(), StageType.RequirementsDiscovery, "user-1");
        var existing = Artefact.CreateS3Artefact(
            command.ProjectId,
            2,
            "session-close/SESSION-CLOSE-P01.md",
            "projects/p/session-close/v2",
            "text/markdown",
            50,
            "user-0",
            _timeProvider,
            true);

        _artefactRepository
            .Setup(repository => repository.GetByProjectAndFilePathAsync(command.ProjectId, "session-close/SESSION-CLOSE-P01.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(3, result.Version);
        _artefactRepository.Verify(repository => repository.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _artefactRepository.Verify(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FilePath_MatchesStageType()
    {
        var command = new GenerateSessionCloseCommand(Guid.NewGuid(), Guid.NewGuid(), StageType.ClinicalSafety, "user-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("session-close/SESSION-CLOSE-P06.md", result.FilePath);
    }

    [Fact]
    public async Task Handle_SkillBuilderCalled_WithStageType()
    {
        var command = new GenerateSessionCloseCommand(Guid.NewGuid(), Guid.NewGuid(), StageType.RequirementsDiscovery, "user-1");

        await _handler.Handle(command, CancellationToken.None);

        _sessionCloseSkillBuilder.Verify(builder => builder.Build(
            StageType.RequirementsDiscovery,
            It.Is<string>(summary => summary.Contains("User message", StringComparison.Ordinal))), Times.Once);
        _aiService.Verify(service => service.GenerateResponseAsync(
            It.Is<AiSystemPrompt>(prompt => prompt.StablePart.Contains("skill prompt", StringComparison.Ordinal)),
            It.IsAny<IReadOnlyList<AiMessage>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ArtefactIsPublished_RaisesEvent()
    {
        var command = new GenerateSessionCloseCommand(Guid.NewGuid(), Guid.NewGuid(), StageType.RequirementsDiscovery, "user-1");
        Artefact? savedArtefact = null;

        _artefactRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Callback<Artefact, CancellationToken>((artefact, _) =>
            {
                savedArtefact = artefact;
            })
            .Returns(Task.CompletedTask);

        await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(savedArtefact);
        Assert.True(savedArtefact!.IsPublished);
    }

    private Conversation CreateConversation(Guid conversationId)
    {
        var conversation = new Conversation(Guid.NewGuid(), 13, _timeProvider);

        conversation.AddMessage(MessageRole.User, "User message 1", null, _timeProvider, "user-1", "A", "B");
        conversation.AddMessage(MessageRole.Assistant, "Assistant response", null, _timeProvider);
        conversation.AddMessage(MessageRole.User, "User message 2", null, _timeProvider, "user-1", "A", "B");

        return conversation;
    }
}