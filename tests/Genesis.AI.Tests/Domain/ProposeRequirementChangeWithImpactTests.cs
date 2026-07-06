using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Commands.ProposeRequirementChange;
using Genesis.AI.Domain.Interfaces;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Domain;

public class ProposeRequirementChangeWithImpactTests
{
    [Fact]
    public async Task ProposeChange_WhenAgentClassifiesCsImpact_StoresCsImpactOnPendingChange()
    {
        var repoMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        repoMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        RequirementChange? savedChange = null;
        repoMock.Setup(r => r.AddAsync(It.IsAny<RequirementChange>(), It.IsAny<CancellationToken>()))
            .Callback<RequirementChange, CancellationToken>((c, _) => savedChange = c);

        var handler = new ProposeRequirementChangeCommandHandler(repoMock.Object);

        var command = new ProposeRequirementChangeCommand(
            ProjectId: Guid.NewGuid(),
            ReqId: "REQ-001",
            ChangeType: ChangeType.Gap,
            RaisingPipeline: "pipeline_01_requirements_discovery",
            RaisingPipelineConversationId: null,
            ProposedAcText: "- [ ] System must block filing when no patient match exists.",
            Rationale: "DCB0129 foreseeable hazard",
            CreatedBy: "pipeline_01",
            ClinicalSafetyImpact: ImpactLevel.Possible,
            IgImpact: ImpactLevel.None,
            SecurityImpact: ImpactLevel.None);

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(savedChange);
        Assert.Equal(ImpactLevel.Possible, savedChange!.ClinicalSafetyImpact);
        Assert.Equal(ImpactLevel.None, savedChange.IgImpact);
        Assert.Equal(ImpactLevel.None, savedChange.SecurityImpact);
        Assert.Equal(ChangeStatus.Pending, savedChange.Status);
    }

    [Fact]
    public async Task ProposeChange_WhenAgentClassifiesIgImpact_StoresIgImpactOnPendingChange()
    {
        var repoMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        repoMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        RequirementChange? savedChange = null;
        repoMock.Setup(r => r.AddAsync(It.IsAny<RequirementChange>(), It.IsAny<CancellationToken>()))
            .Callback<RequirementChange, CancellationToken>((c, _) => savedChange = c);

        var handler = new ProposeRequirementChangeCommandHandler(repoMock.Object);

        var command = new ProposeRequirementChangeCommand(
            ProjectId: Guid.NewGuid(),
            ReqId: "REQ-002",
            ChangeType: ChangeType.Gap,
            RaisingPipeline: "pipeline_07_information_governance",
            RaisingPipelineConversationId: null,
            ProposedAcText: "- [ ] Consent must be recorded before processing.",
            Rationale: "UK GDPR Article 9 — no consent mechanism",
            CreatedBy: "pipeline_07",
            ClinicalSafetyImpact: ImpactLevel.None,
            IgImpact: ImpactLevel.Definite,
            SecurityImpact: ImpactLevel.None);

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(savedChange);
        Assert.Equal(ImpactLevel.None, savedChange!.ClinicalSafetyImpact);
        Assert.Equal(ImpactLevel.Definite, savedChange.IgImpact);
        Assert.Equal(ImpactLevel.None, savedChange.SecurityImpact);
    }

    [Fact]
    public async Task ProposeChange_WhenNoImpactSpecified_DefaultsToNoneForAllDomains()
    {
        var repoMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        repoMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        RequirementChange? savedChange = null;
        repoMock.Setup(r => r.AddAsync(It.IsAny<RequirementChange>(), It.IsAny<CancellationToken>()))
            .Callback<RequirementChange, CancellationToken>((c, _) => savedChange = c);

        var handler = new ProposeRequirementChangeCommandHandler(repoMock.Object);

        var command = new ProposeRequirementChangeCommand(
            ProjectId: Guid.NewGuid(),
            ReqId: "REQ-003",
            ChangeType: ChangeType.Clarification,
            RaisingPipeline: "pipeline_03_architecture",
            RaisingPipelineConversationId: null,
            ProposedAcText: "- [ ] Clarified wording.",
            Rationale: "Ambiguous phrasing — no domain impact",
            CreatedBy: "pipeline_03");

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(savedChange);
        Assert.Equal(ImpactLevel.None, savedChange!.ClinicalSafetyImpact);
        Assert.Equal(ImpactLevel.None, savedChange.IgImpact);
        Assert.Equal(ImpactLevel.None, savedChange.SecurityImpact);
    }
}
