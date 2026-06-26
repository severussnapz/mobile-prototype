using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetArtefactById;
using Genesis.AI.Domain.Queries.GetArtefactsByStage;
using Genesis.AI.Domain.Queries.GetConversationProgress;
using Genesis.AI.Domain.Queries.GetConversationsByStage;
using Genesis.AI.Domain.Queries.GetParkingLot;
using Genesis.AI.Domain.Queries.GetProjectDecisions;
using Genesis.AI.Domain.Queries.GetProjectNotes;
using Genesis.AI.Domain.Queries.GetProjectParkingLot;
using Moq;

namespace Genesis.AI.Tests.Queries;

public class GetParkingLotQueryHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNull()
    {
        var query = new GetParkingLotQuery(Guid.NewGuid());
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(query.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var handler = new GetParkingLotQueryHandler(_conversationRepositoryMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ConversationFound_ReturnsParkingLotItems()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        conversation.AddParkingLotItem("Deferred topic", ParkingLotPriority.High, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var handler = new GetParkingLotQueryHandler(_conversationRepositoryMock.Object);

        var result = await handler.Handle(new GetParkingLotQuery(conversation.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
    }
}

public class GetProjectParkingLotQueryHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task Handle_ItemsExist_ReturnsItemsFromRepository()
    {
        var projectId = Guid.NewGuid();
        var item = new ParkingLotItem(Guid.NewGuid(), "Item", ParkingLotPriority.Medium, 1, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetParkingLotByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var handler = new GetProjectParkingLotQueryHandler(_conversationRepositoryMock.Object);

        var result = await handler.Handle(new GetProjectParkingLotQuery(projectId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
    }
}

public class GetArtefactByIdQueryHandlerTests
{
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task Handle_ArtefactNotFound_ReturnsNull()
    {
        var query = new GetArtefactByIdQuery(Guid.NewGuid());
        _artefactRepositoryMock
            .Setup(repository => repository.GetByIdAsync(query.ArtefactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var handler = new GetArtefactByIdQueryHandler(_artefactRepositoryMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ArtefactFound_ReturnsArtefact()
    {
        var artefact = Artefact.CreateS3Artefact(
            Guid.NewGuid(), 1, "manifest.md", "s3-key", "text/markdown", 10, "user-1", _timeProvider, true);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByIdAsync(artefact.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artefact);

        var handler = new GetArtefactByIdQueryHandler(_artefactRepositoryMock.Object);

        var result = await handler.Handle(new GetArtefactByIdQuery(artefact.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(artefact.Id, result.Id);
    }
}

public class GetArtefactsByStageQueryHandlerTests
{
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task Handle_ArtefactsExist_ReturnsArtefactsForProject()
    {
        var projectId = Guid.NewGuid();
        var artefact = Artefact.CreateS3Artefact(
            projectId, 1, "manifest.md", "s3-key", "text/markdown", 10, "user-1", _timeProvider, true);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([artefact]);

        var handler = new GetArtefactsByStageQueryHandler(_artefactRepositoryMock.Object);

        var result = await handler.Handle(new GetArtefactsByStageQuery(projectId), CancellationToken.None);

        Assert.Single(result);
    }
}

public class GetProjectNotesQueryHandlerTests
{
    private readonly Mock<IProjectNoteRepository> _noteRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task Handle_ProjectDoesNotExist_ReturnsNull()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.ExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new GetProjectNotesQueryHandler(_noteRepositoryMock.Object, _projectRepositoryMock.Object);

        var result = await handler.Handle(new GetProjectNotesQuery(projectId), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ProjectExists_ReturnsNotes()
    {
        var projectId = Guid.NewGuid();
        var note = new ProjectNote(projectId, "A note", "ern-1", "Given", "Family", _timeProvider);
        _projectRepositoryMock
            .Setup(repository => repository.ExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _noteRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([note]);

        var handler = new GetProjectNotesQueryHandler(_noteRepositoryMock.Object, _projectRepositoryMock.Object);

        var result = await handler.Handle(new GetProjectNotesQuery(projectId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
    }
}

public class GetProjectDecisionsQueryHandlerTests
{
    private readonly Mock<IProjectDecisionRepository> _decisionRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task Handle_ProjectDoesNotExist_ReturnsNull()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.ExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new GetProjectDecisionsQueryHandler(_decisionRepositoryMock.Object, _projectRepositoryMock.Object);

        var result = await handler.Handle(new GetProjectDecisionsQuery(projectId), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ProjectExists_ReturnsDecisions()
    {
        var projectId = Guid.NewGuid();
        var decision = new ProjectDecision(
            projectId, "Title", "Context", "Decision", "Consequences", "ern-1", "Given", "Family", _timeProvider);
        _projectRepositoryMock
            .Setup(repository => repository.ExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _decisionRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([decision]);

        var handler = new GetProjectDecisionsQueryHandler(_decisionRepositoryMock.Object, _projectRepositoryMock.Object);

        var result = await handler.Handle(new GetProjectDecisionsQuery(projectId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
    }
}

public class GetConversationProgressQueryHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock = new();
    private readonly Mock<IPromptService> _promptServiceMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNull()
    {
        var query = new GetConversationProgressQuery(Guid.NewGuid());
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(query.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var handler = new GetConversationProgressQueryHandler(_conversationRepositoryMock.Object, _promptServiceMock.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ConversationFound_ReturnsProgressWithPhaseNames()
    {
        var conversation = new Conversation(Guid.NewGuid(), 3, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _conversationRepositoryMock
            .Setup(repository => repository.GetStageTypeByStageIdAsync(conversation.StageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageType.RequirementsDiscovery);
        _promptServiceMock
            .Setup(service => service.GetPhaseNames(StageType.RequirementsDiscovery))
            .Returns(["intro", "discovery", "review"]);

        var handler = new GetConversationProgressQueryHandler(_conversationRepositoryMock.Object, _promptServiceMock.Object);

        var result = await handler.Handle(new GetConversationProgressQuery(conversation.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.PhaseNames.Length);
        Assert.Equal(conversation.TotalPhases, result.TotalPhases);
    }

    [Fact]
    public async Task Handle_StageTypeMissing_ReturnsUnknownPhaseNames()
    {
        var conversation = new Conversation(Guid.NewGuid(), 3, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _conversationRepositoryMock
            .Setup(repository => repository.GetStageTypeByStageIdAsync(conversation.StageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageType?)null);

        var handler = new GetConversationProgressQueryHandler(_conversationRepositoryMock.Object, _promptServiceMock.Object);

        var result = await handler.Handle(new GetConversationProgressQuery(conversation.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(["unknown"], result.PhaseNames);
    }
}

public class GetConversationsByStageQueryHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task Handle_ConversationsExist_ReturnsConversationsForStage()
    {
        var stageId = Guid.NewGuid();
        var conversation = new Conversation(stageId, 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([conversation]);

        var handler = new GetConversationsByStageQueryHandler(_conversationRepositoryMock.Object);

        var result = await handler.Handle(new GetConversationsByStageQuery(stageId), CancellationToken.None);

        Assert.Single(result);
    }
}
