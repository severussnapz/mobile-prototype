using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Commands.GenerateSessionClose;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Genesis.AI.Tests.Domain.Commands.GenerateSessionClose;

public sealed class GenerateSessionCloseCommandHandlerTests
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IAiService _aiService;
    private readonly ISessionCloseSkillBuilder _sessionCloseSkillBuilder;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<GenerateSessionCloseCommandHandler> _logger;
    private readonly GenerateSessionCloseCommandHandler _handler;

    public GenerateSessionCloseCommandHandlerTests()
    {
        _conversationRepository = Substitute.For<IConversationRepository>();
        _artefactRepository = Substitute.For<IArtefactRepository>();
        _artefactStorageService = Substitute.For<IArtefactStorageService>();
        _aiService = Substitute.For<IAiService>();
        _sessionCloseSkillBuilder = Substitute.For<ISessionCloseSkillBuilder>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 07, 07, 9, 0, 0, TimeSpan.Zero));
        _logger = Substitute.For<ILogger<GenerateSessionCloseCommandHandler>>();

        _conversationRepository.GetByIdWithMessagesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => CreateConversation(callInfo.Arg<Guid>()));
        _artefactRepository.GetByProjectAndFilePathAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Artefact?)null);
        _artefactRepository.GetNextVersionForFileAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1);
        _artefactStorageService.SaveContentAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("projects/p/session-close/v1");
        _aiService.GenerateResponseAsync(
                Arg.Any<AiSystemPrompt>(),
                Arg.Any<IReadOnlyList<AiMessage>>(),
                Arg.Any<CancellationToken>())
            .Returns(new AiResponse("# Session Close\n\nSummary content", 1, 1, 0, 0));
        _sessionCloseSkillBuilder.Build(Arg.Any<StageType>(), Arg.Any<string>())
            .Returns("skill prompt");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        _handler = new GenerateSessionCloseCommandHandler(
            _conversationRepository,
            _artefactRepository,
            _artefactStorageService,
            _aiService,
            _sessionCloseSkillBuilder,
            _unitOfWork,
            _timeProvider,
            _logger);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ThrowsNotFoundException()
    {
        var command = new GenerateSessionCloseCommand(Guid.NewGuid(), Guid.NewGuid(), StageType.RequirementsDiscovery, "user-1");
        _conversationRepository.GetByIdWithMessagesAsync(command.ConversationId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task Handle_NewArtefact_CreatesWithVersionOne()
    {
        var command = new GenerateSessionCloseCommand(Guid.NewGuid(), Guid.NewGuid(), StageType.RequirementsDiscovery, "user-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, result.Version);
        await _artefactRepository.Received(1).AddAsync(Arg.Any<Artefact>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        _artefactRepository.GetByProjectAndFilePathAsync(command.ProjectId, "session-close/SESSION-CLOSE-P01.md", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(3, result.Version);
        await _artefactRepository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        await _artefactRepository.DidNotReceive().AddAsync(Arg.Any<Artefact>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        _sessionCloseSkillBuilder.Received(1).Build(
            StageType.RequirementsDiscovery,
            Arg.Is<string>(summary => summary.Contains("User message", StringComparison.Ordinal)));
        await _aiService.Received(1).GenerateResponseAsync(
            Arg.Is<AiSystemPrompt>(prompt => prompt.StablePart.Contains("skill prompt", StringComparison.Ordinal)),
            Arg.Any<IReadOnlyList<AiMessage>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ArtefactIsPublished_RaisesEvent()
    {
        var command = new GenerateSessionCloseCommand(Guid.NewGuid(), Guid.NewGuid(), StageType.RequirementsDiscovery, "user-1");
        Artefact? savedArtefact = null;

        _artefactRepository
            .AddAsync(Arg.Any<Artefact>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                savedArtefact = callInfo.Arg<Artefact>();
                return Task.CompletedTask;
            });

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