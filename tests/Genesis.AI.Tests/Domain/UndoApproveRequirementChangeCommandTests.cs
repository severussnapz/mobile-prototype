using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Commands.UndoApproveRequirementChange;
using Genesis.AI.Domain.Interfaces;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Domain;

public class UndoApproveRequirementChangeCommandTests
{
    [Fact]
    public async Task Handle_WhenApprovedChange_RestoresPreviousReqVersionAndSetsPendingStatus()
    {
        var projectId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var change = BuildApprovedChange(projectId, changeId);

        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        var artefactRepositoryMock = new Mock<IArtefactRepository>();
        var artefactStorageMock = new Mock<IArtefactStorageService>();

        repositoryMock.Setup(r => r.GetByIdForProjectAsync(changeId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(change);
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var reqFilePath = "requirements/REQ-001-unified-inbound-inbox.md";

        var previousArtefact = Artefact.CreateS3Artefact(
            projectId, 1, reqFilePath,
            "s3-key-v1", "text/markdown", 100, "idris.issa", TimeProvider.System, true);
        var currentArtefact = Artefact.CreateS3Artefact(
            projectId, 2, reqFilePath,
            "s3-key-v2", "text/markdown", 120, "idris.issa", TimeProvider.System, true);

        artefactRepositoryMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "requirements/REQ-001.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);
        artefactRepositoryMock
            .Setup(r => r.GetProjectArtefactManifestAsync(
                projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { currentArtefact });
        artefactRepositoryMock
            .Setup(r => r.GetPreviousVersionAsync(
                projectId, reqFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousArtefact);
        artefactRepositoryMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, reqFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentArtefact);
        artefactStorageMock
            .Setup(s => s.GetContentAsync("s3-key-v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("# REQ-001 previous content");
        artefactStorageMock
            .Setup(s => s.SaveContentAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-key-v3");
        artefactRepositoryMock
            .Setup(r => r.GetNextVersionForFileAsync(It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var handler = new UndoApproveRequirementChangeCommandHandler(
            repositoryMock.Object,
            artefactRepositoryMock.Object,
            artefactStorageMock.Object,
            TimeProvider.System);

        var command = new UndoApproveRequirementChangeCommand(
            ProjectId: projectId,
            ChangeId: changeId,
            UndoneBy: "idris.issa",
            UndoRationale: "Wrong wording");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ChangeStatus.Pending, change.Status);
        Assert.Equal("idris.issa", change.UndoneBy);
        Assert.Equal("Wrong wording", change.UndoRationale);
        artefactStorageMock.Verify(s => s.GetContentAsync("s3-key-v1",
            It.IsAny<CancellationToken>()), Times.Once);
        artefactStorageMock.Verify(s => s.SaveContentAsync(
            projectId, reqFilePath, 3,
            "# REQ-001 previous content", "text/markdown",
            It.IsAny<CancellationToken>()), Times.Once);
        artefactRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Artefact>(artefact =>
                artefact.ProjectId == projectId &&
                artefact.Version == 3 &&
                artefact.FilePath == reqFilePath &&
                artefact.S3Key == "s3-key-v3"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenChangeNotFound_ThrowsInvalidOperationException()
    {
        var repositoryMock = new Mock<IRequirementChangeRepository>();
        repositoryMock.Setup(r => r.GetByIdForProjectAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequirementChange?)null);

        var handler = new UndoApproveRequirementChangeCommandHandler(
            repositoryMock.Object,
            new Mock<IArtefactRepository>().Object,
            new Mock<IArtefactStorageService>().Object,
            TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new UndoApproveRequirementChangeCommand(
                ProjectId: Guid.NewGuid(),
                ChangeId: Guid.NewGuid(),
                UndoneBy: "idris.issa",
                UndoRationale: null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNoPreviousVersion_UndoesStatusWithoutRestoringFile()
    {
        var projectId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var change = BuildApprovedChange(projectId, changeId);

        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        var artefactRepositoryMock = new Mock<IArtefactRepository>();
        var artefactStorageMock = new Mock<IArtefactStorageService>();

        repositoryMock.Setup(r => r.GetByIdForProjectAsync(changeId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(change);
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var reqFilePath = "requirements/REQ-001-unified-inbound-inbox.md";
        var currentArtefact = Artefact.CreateS3Artefact(
            projectId, 2, reqFilePath,
            "s3-key-v2", "text/markdown", 120, "idris.issa", TimeProvider.System, true);

        artefactRepositoryMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "requirements/REQ-001.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);
        artefactRepositoryMock
            .Setup(r => r.GetProjectArtefactManifestAsync(
                projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { currentArtefact });
        artefactRepositoryMock
            .Setup(r => r.GetPreviousVersionAsync(
                projectId, reqFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var handler = new UndoApproveRequirementChangeCommandHandler(
            repositoryMock.Object,
            artefactRepositoryMock.Object,
            artefactStorageMock.Object,
            TimeProvider.System);

        await handler.Handle(new UndoApproveRequirementChangeCommand(
            ProjectId: projectId,
            ChangeId: changeId,
            UndoneBy: "idris.issa",
            UndoRationale: null),
        CancellationToken.None);

        Assert.Equal(ChangeStatus.Pending, change.Status);
        artefactStorageMock.Verify(s => s.SaveContentAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoMatchingRequirementArtefactPath_UndoesStatusWithoutRestoringFile()
    {
        var projectId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var change = BuildApprovedChange(projectId, changeId);

        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        var artefactRepositoryMock = new Mock<IArtefactRepository>();
        var artefactStorageMock = new Mock<IArtefactStorageService>();

        repositoryMock.Setup(r => r.GetByIdForProjectAsync(changeId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(change);
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        artefactRepositoryMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "requirements/REQ-001.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);
        artefactRepositoryMock
            .Setup(r => r.GetProjectArtefactManifestAsync(
                projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact>
            {
                Artefact.CreateS3Artefact(
                    projectId,
                    1,
                    "prototype/index.html",
                    "s3-prototype",
                    "text/html",
                    200,
                    "idris.issa",
                    TimeProvider.System,
                    true)
            });

        var handler = new UndoApproveRequirementChangeCommandHandler(
            repositoryMock.Object,
            artefactRepositoryMock.Object,
            artefactStorageMock.Object,
            TimeProvider.System);

        await handler.Handle(new UndoApproveRequirementChangeCommand(
            ProjectId: projectId,
            ChangeId: changeId,
            UndoneBy: "idris.issa",
            UndoRationale: null),
        CancellationToken.None);

        Assert.Equal(ChangeStatus.Pending, change.Status);
        artefactRepositoryMock.Verify(r => r.GetPreviousVersionAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        artefactStorageMock.Verify(s => s.SaveContentAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RequirementChange BuildApprovedChange(Guid projectId, Guid changeId)
    {
        var change = RequirementChange.Propose(
            projectId: projectId,
            reqId: "REQ-001",
            changeType: ChangeType.Gap,
            raisingPipeline: "pipeline_05_pxd",
            raisingPipelineConversationId: null,
            proposedAcText: "[ ] Step indicator shows all steps.",
            rationale: "Missing behaviour",
            createdBy: "idris.issa");

        typeof(RequirementChange)
            .GetProperty("Id")!
            .SetValue(change, changeId);

        change.Approve("[ ] Step indicator shows all steps.",
            ImpactLevel.None, ImpactLevel.None, ImpactLevel.None,
            "idris.issa", TimeProvider.System);

        return change;
    }
}
