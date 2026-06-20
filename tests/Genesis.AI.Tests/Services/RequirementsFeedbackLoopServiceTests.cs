using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.PrototypeLockAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.AggregatesModel.UiDeltaAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Moq;

namespace Genesis.AI.Tests.Services;

public class RequirementsFeedbackLoopServiceTests
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task LockPrototypeAsync_AfterReopen_AppendsOnlyNewUnlockedDeltas()
    {
        var projectId = Guid.NewGuid();
        var requirementId = "REQ-002";
        var requirementFilePath = "requirements/REQ-002.md";
        var lockedBy = "ern:emis:user:user:123";

        var uiDeltaRepositoryMock = new Mock<IUiDeltaRepository>();
        var prototypeLockRepositoryMock = new Mock<IPrototypeLockRepository>();
        var projectRepositoryMock = new Mock<IProjectRepository>();
        var artefactRepositoryMock = new Mock<IArtefactRepository>();
        var artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        var classifierMock = new Mock<IRequirementImpactClassifier>();

        var uiDeltaUnitOfWorkMock = new Mock<IUnitOfWork>();
        var prototypeLockUnitOfWorkMock = new Mock<IUnitOfWork>();
        var artefactUnitOfWorkMock = new Mock<IUnitOfWork>();

        uiDeltaRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(uiDeltaUnitOfWorkMock.Object);
        prototypeLockRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(prototypeLockUnitOfWorkMock.Object);
        artefactRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(artefactUnitOfWorkMock.Object);

        uiDeltaUnitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        prototypeLockUnitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        artefactUnitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var project = new Project("DOC", "Documents", null, "PORTASK0001045", ComplianceDomain.ClinicalUk, "seed", _timeProvider);
        var prototypeStage = project.PipelineStages.First(stage => stage.StageType == StageType.Prototype);

        projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var lockRow = new PrototypeLock(projectId, prototypeStage.Id, _timeProvider);
        lockRow.MarkLocked(DateTimeOffset.UtcNow, "seed");
        lockRow.ClearLock(_timeProvider);
        prototypeLockRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(prototypeStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockRow);

        var secondDelta = new UiDelta(
            projectId,
            prototypeStage.Id,
            requirementId,
            "node-second",
            "prototype/fragments/screen-02-b.html",
            "graph_node_edit",
            "conversation_graph_node_edit",
            "Add warning callout",
            "No warning callout",
            "Warning callout added",
            RequirementImpact.Substantive,
            "seed",
            _timeProvider);

        uiDeltaRepositoryMock
            .Setup(repository => repository.GetUnlockedSubstantiveByRequirementAsync(projectId, requirementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([secondDelta]);

        var requirementArtefact = Artefact.CreateS3Artefact(
            projectId,
            1,
            requirementFilePath,
            "s3-existing",
            "text/markdown",
            32,
            "seed",
            _timeProvider,
            true);

        artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(projectId, requirementFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(requirementArtefact);

        var currentRequirementContent = """
            # Requirement 002

            Initial body.

            ## UI/UX decisions made during prototyping (REQ-002)

            Locked at: 2026-06-16T00:00:00.0000000+00:00
            Lock batch id: 11111111-1111-1111-1111-111111111111

            ### Decision 1
            - Target: node-first
            - Operation: graph_node_edit
            - Source: conversation_graph_node_edit
            - User request: Move action above form
            - Before: Action below form
            - After: Action above form
            """;
        artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => currentRequirementContent);

        artefactRepositoryMock
            .Setup(repository => repository.GetNextVersionForFileAsync(projectId, requirementFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var savedContent = string.Empty;
        artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                projectId,
                requirementFilePath,
                It.IsAny<int>(),
                It.IsAny<string>(),
                "text/markdown",
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>((_, _, _, content, _, _) =>
            {
                savedContent = content;
                currentRequirementContent = content;
            })
            .ReturnsAsync("s3-updated");

        artefactRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactRepositoryMock
            .Setup(repository => repository.DeletePreviousVersionsAsync(projectId, requirementFilePath, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var markedIdBatches = new List<IReadOnlyList<Guid>>();
        uiDeltaRepositoryMock
            .Setup(repository => repository.MarkLockedBatchAsync(
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Guid>, Guid, string, DateTimeOffset, CancellationToken>((ids, _, _, _, _) =>
            {
                markedIdBatches.Add(ids);
            })
            .Returns(Task.CompletedTask);

        var service = new RequirementsFeedbackLoopService(
            uiDeltaRepositoryMock.Object,
            prototypeLockRepositoryMock.Object,
            projectRepositoryMock.Object,
            artefactRepositoryMock.Object,
            artefactStorageServiceMock.Object,
            classifierMock.Object,
            _timeProvider);

        var secondLockResult = await service.LockPrototypeAsync(
            projectId,
            requirementId,
            requirementFilePath,
            lockedBy,
            CancellationToken.None);

        Assert.Equal(1, secondLockResult.AppendedDeltaCount);
        Assert.Contains("node-first", savedContent, StringComparison.Ordinal);
        Assert.Contains("node-second", savedContent, StringComparison.Ordinal);
        Assert.Single(markedIdBatches);
        Assert.Single(markedIdBatches[0]);
        Assert.Contains(secondDelta.Id, markedIdBatches[0]);
    }

    [Fact]
    public async Task LockPrototypeAsync_UnlockedSubstantiveBatch_AppendsAndMarksOnlyCurrentBatch()
    {
        var projectId = Guid.NewGuid();
        var requirementId = "REQ-001";
        var requirementFilePath = "requirements/REQ-001.md";
        var lockedBy = "ern:emis:user:user:123";

        var uiDeltaRepositoryMock = new Mock<IUiDeltaRepository>();
        var prototypeLockRepositoryMock = new Mock<IPrototypeLockRepository>();
        var projectRepositoryMock = new Mock<IProjectRepository>();
        var artefactRepositoryMock = new Mock<IArtefactRepository>();
        var artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        var classifierMock = new Mock<IRequirementImpactClassifier>();

        var uiDeltaUnitOfWorkMock = new Mock<IUnitOfWork>();
        var prototypeLockUnitOfWorkMock = new Mock<IUnitOfWork>();
        var artefactUnitOfWorkMock = new Mock<IUnitOfWork>();

        uiDeltaRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(uiDeltaUnitOfWorkMock.Object);
        prototypeLockRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(prototypeLockUnitOfWorkMock.Object);
        artefactRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(artefactUnitOfWorkMock.Object);

        uiDeltaUnitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        prototypeLockUnitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        artefactUnitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var project = new Project("DOC", "Documents", null, "PORTASK0001045", ComplianceDomain.ClinicalUk, "seed", _timeProvider);
        var prototypeStage = project.PipelineStages.First(stage => stage.StageType == StageType.Prototype);

        projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        prototypeLockRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(prototypeStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PrototypeLock?)null);

        var deltaOne = new UiDelta(
            projectId,
            prototypeStage.Id,
            requirementId,
            "node-a",
            "prototype/fragments/screen-01-a.html",
            "graph_node_edit",
            "conversation_graph_node_edit",
            "Move action above form",
            "Action below form",
            "Action above form",
            RequirementImpact.Substantive,
            "seed",
            _timeProvider);

        var deltaTwo = new UiDelta(
            projectId,
            prototypeStage.Id,
            requirementId,
            "node-b",
            "prototype/fragments/screen-02-b.html",
            "toggle_visibility",
            "structural_edit",
            "Hide optional panel",
            "Panel visible",
            "Panel hidden",
            RequirementImpact.Substantive,
            "seed",
            _timeProvider);

        uiDeltaRepositoryMock
            .Setup(repository => repository.GetUnlockedSubstantiveByRequirementAsync(projectId, requirementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([deltaOne, deltaTwo]);

        var existingRequirementArtefact = Artefact.CreateS3Artefact(
            projectId,
            1,
            requirementFilePath,
            "s3-existing",
            "text/markdown",
            32,
            "seed",
            _timeProvider,
            true);

        artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(projectId, requirementFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRequirementArtefact);

        artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Requirement 001\n\nInitial body.");

        artefactRepositoryMock
            .Setup(repository => repository.GetNextVersionForFileAsync(projectId, requirementFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var savedContent = string.Empty;
        artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                projectId,
                requirementFilePath,
                2,
                It.IsAny<string>(),
                "text/markdown",
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>((_, _, _, content, _, _) => savedContent = content)
            .ReturnsAsync("s3-updated");

        artefactRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactRepositoryMock
            .Setup(repository => repository.DeletePreviousVersionsAsync(projectId, requirementFilePath, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        IReadOnlyList<Guid>? markedIds = null;
        Guid markedBatchId = Guid.Empty;
        string? markedFilePath = null;
        uiDeltaRepositoryMock
            .Setup(repository => repository.MarkLockedBatchAsync(
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Guid>, Guid, string, DateTimeOffset, CancellationToken>((ids, batchId, filePath, _, _) =>
            {
                markedIds = ids;
                markedBatchId = batchId;
                markedFilePath = filePath;
            })
            .Returns(Task.CompletedTask);

        var service = new RequirementsFeedbackLoopService(
            uiDeltaRepositoryMock.Object,
            prototypeLockRepositoryMock.Object,
            projectRepositoryMock.Object,
            artefactRepositoryMock.Object,
            artefactStorageServiceMock.Object,
            classifierMock.Object,
            _timeProvider);

        var result = await service.LockPrototypeAsync(
            projectId,
            requirementId,
            requirementFilePath,
            lockedBy,
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.AppendedDeltaCount);
        Assert.NotEqual(Guid.Empty, result.LockBatchId);
        Assert.Contains("UI/UX decisions made during prototyping", savedContent, StringComparison.Ordinal);
        Assert.Contains("Decision 1", savedContent, StringComparison.Ordinal);
        Assert.Contains("Decision 2", savedContent, StringComparison.Ordinal);

        Assert.NotNull(markedIds);
        Assert.Equal(2, markedIds!.Count);
        Assert.Contains(deltaOne.Id, markedIds);
        Assert.Contains(deltaTwo.Id, markedIds);
        Assert.Equal(result.LockBatchId, markedBatchId);
        Assert.Equal(requirementFilePath, markedFilePath);
    }
}
