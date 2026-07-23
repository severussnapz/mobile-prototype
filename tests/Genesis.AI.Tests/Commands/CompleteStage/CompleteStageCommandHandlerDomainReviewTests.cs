using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Commands.CompleteStage;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class CompleteStageCommandHandlerDomainReviewTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IRequirementChangeRepository> _requirementChangeRepositoryMock;
    private readonly Mock<IUnitOfWork> _projectUnitOfWorkMock;
    private readonly Mock<IUnitOfWork> _requirementChangeUnitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly CompleteStageCommandHandler _handler;

    public CompleteStageCommandHandlerDomainReviewTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _requirementChangeRepositoryMock = new Mock<IRequirementChangeRepository>();
        _projectUnitOfWorkMock = new Mock<IUnitOfWork>();
        _requirementChangeUnitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _projectRepositoryMock
            .Setup(repository => repository.UnitOfWork)
            .Returns(_projectUnitOfWorkMock.Object);

        _projectUnitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _requirementChangeRepositoryMock
            .Setup(repository => repository.UnitOfWork)
            .Returns(_requirementChangeUnitOfWorkMock.Object);

        _requirementChangeUnitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new CompleteStageCommandHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _requirementChangeRepositoryMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenSecurityStageCompletes_RecordsSecurityReviewOnDefiniteChanges()
    {
        var project = CreateProjectWithInProgressStage(StageType.Security);
        var stage = project.PipelineStages.First(pipelineStage => pipelineStage.StageType == StageType.Security);
        var change = CreatePendingChange(project.Id, ImpactLevel.None, ImpactLevel.None, ImpactLevel.Definite);

        _projectRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { CreateArtefact(project.Id) });

        _requirementChangeRepositoryMock
            .Setup(repository => repository.GetPendingByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RequirementChange> { change });

        var command = new CompleteStageCommand(stage.Id, "user-1");

        await _handler.Handle(command, CancellationToken.None);

        Assert.True(change.SecurityReviewed);
        _requirementChangeRepositoryMock.Verify(
            repository => repository.GetPendingByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _requirementChangeUnitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSecurityStageCompletes_DoesNotRecordReview_WhenImpactIsPossible()
    {
        var project = CreateProjectWithInProgressStage(StageType.Security);
        var stage = project.PipelineStages.First(pipelineStage => pipelineStage.StageType == StageType.Security);
        var change = CreatePendingChange(project.Id, ImpactLevel.None, ImpactLevel.None, ImpactLevel.Possible);

        _projectRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { CreateArtefact(project.Id) });

        _requirementChangeRepositoryMock
            .Setup(repository => repository.GetPendingByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RequirementChange> { change });

        var command = new CompleteStageCommand(stage.Id, "user-1");

        await _handler.Handle(command, CancellationToken.None);

        Assert.False(change.SecurityReviewed);
        _requirementChangeRepositoryMock.Verify(
            repository => repository.GetPendingByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _requirementChangeUnitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenClinicalSafetyStageCompletes_RecordsClinicalSafetyReview()
    {
        var project = CreateProjectWithInProgressStage(StageType.ClinicalSafety);
        var stage = project.PipelineStages.First(pipelineStage => pipelineStage.StageType == StageType.ClinicalSafety);
        var change = CreatePendingChange(project.Id, ImpactLevel.Definite, ImpactLevel.None, ImpactLevel.None);

        _projectRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { CreateArtefact(project.Id) });

        _requirementChangeRepositoryMock
            .Setup(repository => repository.GetPendingByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RequirementChange> { change });

        var command = new CompleteStageCommand(stage.Id, "user-1");

        await _handler.Handle(command, CancellationToken.None);

        Assert.True(change.ClinicalSafetyReviewed);
        _requirementChangeRepositoryMock.Verify(
            repository => repository.GetPendingByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _requirementChangeUnitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIgStageCompletes_RecordsIgReview()
    {
        var project = CreateProjectWithInProgressStage(StageType.InformationGovernance);
        var stage = project.PipelineStages.First(pipelineStage => pipelineStage.StageType == StageType.InformationGovernance);
        var change = CreatePendingChange(project.Id, ImpactLevel.None, ImpactLevel.Definite, ImpactLevel.None);

        _projectRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { CreateArtefact(project.Id) });

        _requirementChangeRepositoryMock
            .Setup(repository => repository.GetPendingByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RequirementChange> { change });

        var command = new CompleteStageCommand(stage.Id, "user-1");

        await _handler.Handle(command, CancellationToken.None);

        Assert.True(change.IgReviewed);
        _requirementChangeRepositoryMock.Verify(
            repository => repository.GetPendingByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _requirementChangeUnitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNonDomainStageCompletes_DoesNotQueryRequirementChanges()
    {
        var project = CreateProjectWithInProgressStage(StageType.RequirementsDiscovery);
        var stage = project.PipelineStages.First(pipelineStage => pipelineStage.StageType == StageType.RequirementsDiscovery);

        _projectRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { CreateArtefact(project.Id) });

        var command = new CompleteStageCommand(stage.Id, "user-1");

        await _handler.Handle(command, CancellationToken.None);

        _requirementChangeRepositoryMock.Verify(
            repository => repository.GetPendingByProjectIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private Project CreateProjectWithInProgressStage(StageType stageType)
    {
        var project = new Project("DOC", "Documents", null, "PORTASK0001045", ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        var stage = project.PipelineStages.First(pipelineStage => pipelineStage.StageType == stageType);
        stage.Start(_timeProvider);
        return project;
    }

    private static RequirementChange CreatePendingChange(
        Guid projectId,
        ImpactLevel clinicalSafetyImpact,
        ImpactLevel igImpact,
        ImpactLevel securityImpact)
    {
        return RequirementChange.Propose(
            projectId: projectId,
            reqId: "REQ-001",
            changeType: ChangeType.Gap,
            raisingPipeline: "pipeline_05_pxd",
            raisingPipelineConversationId: null,
            proposedAcText: "[ ] Add requirement clarification",
            rationale: "Normalisation follow-up",
            createdBy: "user-1",
            clinicalSafetyImpact: clinicalSafetyImpact,
            igImpact: igImpact,
            securityImpact: securityImpact);
    }

    private static Artefact CreateArtefact(Guid projectId)
    {
        return Artefact.CreateS3Artefact(
            projectId,
            1,
            "requirements.md",
            "projects/key/artefacts/requirements.md/v1",
            "text/markdown",
            9,
            "user-1",
            TimeProvider.System,
            true);
    }
}
