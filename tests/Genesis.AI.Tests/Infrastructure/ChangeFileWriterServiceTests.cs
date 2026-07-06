using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class ChangeFileWriterServiceTests
{
    [Fact]
    public async Task WriteChangeFileAsync_WhenGapApproved_WritesCorrectContent()
    {
        var projectId = Guid.NewGuid();
        var change = BuildApprovedChange(projectId, ChangeType.Gap,
            ImpactLevel.None, ImpactLevel.None, ImpactLevel.None);

        var artefactRepositoryMock = new Mock<IArtefactRepository>();
        var artefactStorageMock = new Mock<IArtefactStorageService>();

        artefactRepositoryMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Genesis.AI.Domain.AggregatesModel.ArtefactAggregate.Artefact?)null);
        artefactRepositoryMock
            .Setup(r => r.GetNextVersionForFileAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        artefactStorageMock
            .Setup(s => s.SaveContentAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-key");

        string? savedContent = null;
        string? savedPath = null;
        artefactStorageMock
            .Setup(s => s.SaveContentAsync(projectId, It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>(
                (_, path, _, content, _, _) =>
                {
                    savedPath = path;
                    savedContent = content;
                })
            .ReturnsAsync("s3-key");

        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        artefactRepositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new ChangeFileWriterService(
            artefactRepositoryMock.Object,
            artefactStorageMock.Object,
            TimeProvider.System);

        await service.WriteChangeFileAsync(change, CancellationToken.None);

        Assert.NotNull(savedContent);
        Assert.NotNull(savedPath);
        Assert.StartsWith("changes/", savedPath);
        Assert.Contains("REQ-001", savedContent!);
        Assert.Contains("GAP", savedContent);
        Assert.Contains("pipeline_05_pxd", savedContent);
        Assert.Contains("Missing behaviour", savedContent);
        Assert.Contains("Clinical Safety: None", savedContent);
        Assert.Contains("IG: None", savedContent);
        Assert.Contains("Security: None", savedContent);
    }

    [Fact]
    public async Task WriteChangeFileAsync_WhenDefiniteIgImpact_IncludesReviewRequiredSection()
    {
        var projectId = Guid.NewGuid();
        var change = BuildApprovedChange(projectId, ChangeType.Gap,
            ImpactLevel.None, ImpactLevel.Definite, ImpactLevel.None);

        var artefactRepositoryMock = new Mock<IArtefactRepository>();
        var artefactStorageMock = new Mock<IArtefactStorageService>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();

        artefactRepositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        artefactRepositoryMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Genesis.AI.Domain.AggregatesModel.ArtefactAggregate.Artefact?)null);
        artefactRepositoryMock
            .Setup(r => r.GetNextVersionForFileAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        string? savedContent = null;
        artefactStorageMock
            .Setup(s => s.SaveContentAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>(
                (_, _, _, content, _, _) => savedContent = content)
            .ReturnsAsync("s3-key");

        var service = new ChangeFileWriterService(
            artefactRepositoryMock.Object,
            artefactStorageMock.Object,
            TimeProvider.System);

        await service.WriteChangeFileAsync(change, CancellationToken.None);

        Assert.NotNull(savedContent);
        Assert.Contains("IG: Definite — review required", savedContent!);
        Assert.Contains("Reviews pending", savedContent);
    }

    private static RequirementChange BuildApprovedChange(
        Guid projectId,
        ChangeType changeType,
        ImpactLevel clinicalSafety,
        ImpactLevel ig,
        ImpactLevel security)
    {
        var change = RequirementChange.Propose(
            projectId: projectId,
            reqId: "REQ-001",
            changeType: changeType,
            raisingPipeline: "pipeline_05_pxd",
            raisingPipelineConversationId: null,
            proposedAcText: "[ ] Step indicator shows all steps.",
            rationale: "Missing behaviour",
            createdBy: "idris.issa");

        change.Approve("[ ] Step indicator shows all steps.",
            clinicalSafety, ig, security, "idris.issa", TimeProvider.System);

        return change;
    }
}
