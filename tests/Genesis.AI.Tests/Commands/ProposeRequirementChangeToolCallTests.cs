using System.Text.Json;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Commands.ProposeRequirementChange;
using Genesis.AI.Domain.Interfaces;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Commands;

public class ProposeRequirementChangeToolCallTests
{
    [Fact]
    public async Task ProposeRequirementChange_WhenValidGapInput_SavesPendingChange()
    {
        var projectId = Guid.NewGuid();
        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        RequirementChange? savedChange = null;
        repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<RequirementChange>(), It.IsAny<CancellationToken>()))
            .Callback<RequirementChange, CancellationToken>((c, _) => savedChange = c);

        var handler = new ProposeRequirementChangeCommandHandler(repositoryMock.Object);
        var command = new ProposeRequirementChangeCommand(
            ProjectId: projectId,
            ReqId: "REQ-001",
            ChangeType: ChangeType.Gap,
            RaisingPipeline: "pipeline_01_requirements_discovery",
            RaisingPipelineConversationId: null,
            ProposedAcText: "- [ ] System must block filing when no patient match exists.",
            Rationale: "No AC covers this error state",
            CreatedBy: "test-user");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.ChangeId);
        Assert.NotNull(savedChange);
        Assert.Equal(ChangeType.Gap, savedChange!.ChangeType);
        Assert.Equal("REQ-001", savedChange.ReqId);
        Assert.Equal(ChangeStatus.Pending, savedChange.Status);
    }

    [Fact]
    public async Task ProposeRequirementChange_WhenContradictionWithNullAcText_Succeeds()
    {
        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ProposeRequirementChangeCommandHandler(repositoryMock.Object);
        var command = new ProposeRequirementChangeCommand(
            ProjectId: Guid.NewGuid(),
            ReqId: "REQ-001",
            ChangeType: ChangeType.Contradiction,
            RaisingPipeline: "pipeline_05_pxd",
            RaisingPipelineConversationId: null,
            ProposedAcText: null,
            Rationale: "REQ-001 says X but REQ-007 says Y — direct conflict",
            CreatedBy: "test-user");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.ChangeId);
    }
}
