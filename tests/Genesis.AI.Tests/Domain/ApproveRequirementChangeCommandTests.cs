using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Commands.ApproveRequirementChange;
using Genesis.AI.Domain.Interfaces;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Domain;

public class ApproveRequirementChangeCommandTests
{
    [Fact]
    public async Task Handle_WhenPendingChange_ApprovesAndInsertsAcText()
    {
        var projectId = Guid.NewGuid();
        var changeId = Guid.NewGuid();

        var change = BuildPendingChange(projectId, changeId,
            "[ ] Step indicator shows all steps.");

        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        var artefactRepositoryMock = new Mock<IArtefactRepository>();
        var artefactStorageMock = new Mock<IArtefactStorageService>();

        repositoryMock.Setup(r => r.GetByIdAsync(changeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(change);
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var reqContent = """
            # REQ-001: Title

            ## User Story
            As a user I need something.

            ## Acceptance Criteria

            - [ ] Existing AC item one. *(Must Have)*
            - [ ] Existing AC item two. *(Must Have)*

            ## Clinical Safety
            """;

        var realArtefact = Genesis.AI.Domain.AggregatesModel.ArtefactAggregate.Artefact.CreateS3Artefact(
            projectId, 1, "requirements/REQ-001.md", "s3-key-v1", "text/markdown", 100, "idris.issa", TimeProvider.System, true);
        artefactRepositoryMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "requirements/REQ-001.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync(realArtefact);
        artefactStorageMock
            .Setup(s => s.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reqContent);

        string? savedContent = null;
        artefactStorageMock
            .Setup(s => s.SaveContentAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>(
                (_, _, _, content, _, _) => savedContent = content)
            .ReturnsAsync("s3-key");
        artefactRepositoryMock
            .Setup(r => r.GetNextVersionForFileAsync(It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var handler = new ApproveRequirementChangeCommandHandler(
            repositoryMock.Object,
            artefactRepositoryMock.Object,
            artefactStorageMock.Object,
            TimeProvider.System);

        var command = new ApproveRequirementChangeCommand(
            ChangeId: changeId,
            ApprovedAcText: null,
            ClinicalSafetyImpact: ImpactLevel.None,
            IgImpact: ImpactLevel.None,
            SecurityImpact: ImpactLevel.None,
            ApprovedBy: "idris.issa");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ChangeStatus.Approved, change.Status);
        Assert.NotNull(savedContent);
        Assert.Contains("[ ] Step indicator shows all steps.", savedContent!);
        Assert.Contains("*(Added by CHANGE-", savedContent);
        Assert.Contains("- [ ] Existing AC item two.", savedContent);
    }

    [Fact]
    public async Task Handle_WhenHumanEditsAcText_SetsHumanEdited()
    {
        var projectId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var change = BuildPendingChange(projectId, changeId,
            "[ ] Original agent text.");

        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        var artefactRepositoryMock = new Mock<IArtefactRepository>();
        var artefactStorageMock = new Mock<IArtefactStorageService>();

        repositoryMock.Setup(r => r.GetByIdAsync(changeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(change);
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        SetupArtefactMocks(projectId, artefactRepositoryMock, artefactStorageMock,
            "# REQ\n## Acceptance Criteria\n- [ ] Existing.\n## Clinical Safety\n");

        var handler = new ApproveRequirementChangeCommandHandler(
            repositoryMock.Object,
            artefactRepositoryMock.Object,
            artefactStorageMock.Object,
            TimeProvider.System);

        var command = new ApproveRequirementChangeCommand(
            ChangeId: changeId,
            ApprovedAcText: "[ ] Human corrected text.",
            ClinicalSafetyImpact: ImpactLevel.None,
            IgImpact: ImpactLevel.None,
            SecurityImpact: ImpactLevel.None,
            ApprovedBy: "idris.issa");

        await handler.Handle(command, CancellationToken.None);

        Assert.True(change.HumanEdited);
        Assert.Equal("[ ] Human corrected text.", change.ApprovedAcText);
    }

    [Fact]
    public async Task Handle_WhenChangeNotFound_ThrowsInvalidOperationException()
    {
        var repositoryMock = new Mock<IRequirementChangeRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequirementChange?)null);

        var handler = new ApproveRequirementChangeCommandHandler(
            repositoryMock.Object,
            new Mock<IArtefactRepository>().Object,
            new Mock<IArtefactStorageService>().Object,
            TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ApproveRequirementChangeCommand(
                ChangeId: Guid.NewGuid(),
                ApprovedAcText: null,
                ClinicalSafetyImpact: ImpactLevel.None,
                IgImpact: ImpactLevel.None,
                SecurityImpact: ImpactLevel.None,
                ApprovedBy: "idris.issa"),
            CancellationToken.None));
    }

    [Fact]
    public void AcInsertionLogic_WhenMultipleAcBlocks_InsertsAfterLastAcItem()
    {
        var content = """
            # REQ-001

            ## Acceptance Criteria

            - [ ] First item.
            - [ ] Second item.

            ## Clinical Safety
            """;

        var result = AcInsertionHelper.InsertAcText(
            content, "[ ] New item.", "CHG-001", "pipeline_05");

        var lines = result.Split('\n');
        var secondItemIndex = Array.FindIndex(lines,
            l => l.Contains("Second item."));
        var newItemIndex = Array.FindIndex(lines,
            l => l.Contains("New item."));

        Assert.True(newItemIndex > secondItemIndex,
            "New AC item should appear after the last existing AC item");
        Assert.True(result.IndexOf("New item.", StringComparison.Ordinal) <
            result.IndexOf("## Clinical Safety", StringComparison.Ordinal),
            "New AC item should appear before ## Clinical Safety");
    }

    [Fact]
    public void AcInsertionLogic_WhenNoAcSection_ThrowsInvalidOperationException()
    {
        var content = "# REQ-001\n## User Story\nAs a user.\n## Clinical Safety\n";

        Assert.Throws<InvalidOperationException>(() =>
            AcInsertionHelper.InsertAcText(content, "[ ] New.", "CHG-001", "pipeline_05"));
    }

    private static RequirementChange BuildPendingChange(
        Guid projectId, Guid changeId, string proposedAcText)
    {
        var change = RequirementChange.Propose(
            projectId: projectId,
            reqId: "REQ-001",
            changeType: ChangeType.Gap,
            raisingPipeline: "pipeline_05_pxd",
            raisingPipelineConversationId: null,
            proposedAcText: proposedAcText,
            rationale: "Missing behaviour",
            createdBy: "idris.issa");

        // Force the ID via reflection for test predictability
        typeof(RequirementChange)
            .GetProperty("Id")!
            .SetValue(change, changeId);

        return change;
    }

    private static void SetupArtefactMocks(
        Guid projectId,
        Mock<IArtefactRepository> artefactRepositoryMock,
        Mock<IArtefactStorageService> artefactStorageMock,
        string reqContent)
    {
        var realArtefact = Genesis.AI.Domain.AggregatesModel.ArtefactAggregate.Artefact.CreateS3Artefact(
            projectId, 1, "requirements/REQ-001.md", "s3-key-v1", "text/markdown", 100, "idris.issa", TimeProvider.System, true);
        artefactRepositoryMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "requirements/REQ-001.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync(realArtefact);
        artefactStorageMock
            .Setup(s => s.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reqContent);
        artefactStorageMock
            .Setup(s => s.SaveContentAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-key");
        artefactRepositoryMock
            .Setup(r => r.GetNextVersionForFileAsync(It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
    }
}
