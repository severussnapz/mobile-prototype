using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.CreateConversation;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class CreateConversationCommandHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IPromptService> _promptServiceMock;
    private readonly Mock<IUnitOfWork> _conversationUnitOfWorkMock;
    private readonly Mock<IUnitOfWork> _projectUnitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly CreateConversationCommandHandler _handler;

    public CreateConversationCommandHandlerTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _promptServiceMock = new Mock<IPromptService>();
        _conversationUnitOfWorkMock = new Mock<IUnitOfWork>();
        _projectUnitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _conversationRepositoryMock
            .Setup(r => r.UnitOfWork)
            .Returns(_conversationUnitOfWorkMock.Object);

        _projectRepositoryMock
            .Setup(r => r.UnitOfWork)
            .Returns(_projectUnitOfWorkMock.Object);

        _conversationUnitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _projectUnitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _promptServiceMock
            .Setup(p => p.GetTotalPhases(It.IsAny<StageType>()))
            .Returns(5);

        _handler = new CreateConversationCommandHandler(
            _conversationRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _promptServiceMock.Object,
            _timeProvider);
    }

    private Project CreateProjectWithStages(ComplianceDomain domain = ComplianceDomain.ClinicalUk)
    {
        return new Project("DOC", "Documents", null, "PORTASK0001045", domain, "user-1", _timeProvider);
    }

    [Fact]
    public async Task Handle_ValidStage_ReturnsNewConversationId()
    {
        var project = CreateProjectWithStages();
        var reqStage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(reqStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CreateConversationCommand(reqStage.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task Handle_NotStartedStage_StartsStage()
    {
        var project = CreateProjectWithStages();
        var reqStage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(reqStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CreateConversationCommand(reqStage.Id);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(PipelineStageStatus.InProgress, reqStage.Status);
    }

    [Fact]
    public async Task Handle_CompletedStage_ReopensStage()
    {
        var project = CreateProjectWithStages();
        var reqStage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);
        reqStage.Start(_timeProvider);
        reqStage.Complete("user-1", _timeProvider);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(reqStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CreateConversationCommand(reqStage.Id);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(PipelineStageStatus.InProgress, reqStage.Status);
        Assert.Equal(2, reqStage.Iteration);
    }

    [Fact]
    public async Task Handle_BlockedStage_ThrowsInvalidOperationException()
    {
        var project = CreateProjectWithStages(ComplianceDomain.Generic);
        var clinicalStage = project.PipelineStages.First(s => s.StageType == StageType.ClinicalSafety);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(clinicalStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CreateConversationCommand(clinicalStage.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("blocked", exception.Message);
    }

    [Fact]
    public async Task Handle_NonReqStageBlocked_ThrowsInvalidOperationException()
    {
        var project = CreateProjectWithStages();
        var protoStage = project.PipelineStages.First(s => s.StageType == StageType.Prototype);
        // Prototype is Blocked because RequirementsDiscovery not complete

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(protoStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CreateConversationCommand(protoStage.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("blocked", exception.Message);
    }

    [Fact]
    public async Task Handle_BlockedStageContinuationRequest_Succeeds()
    {
        var project = CreateProjectWithStages();
        var igStage = project.PipelineStages.First(stage => stage.StageType == StageType.InformationGovernance);
        var priorConversation = new Conversation(igStage.Id, 5, _timeProvider);

        _projectRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(igStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(priorConversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priorConversation);

        var command = new CreateConversationCommand(igStage.Id, null, priorConversation.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        _conversationRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Conversation>(conversation =>
                    conversation.ContinuedFromConversationId == priorConversation.Id &&
                    conversation.StageId == igStage.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ContinuationWithMismatchedStage_ThrowsInvalidOperationException()
    {
        var project = CreateProjectWithStages();
        var requestedStage = project.PipelineStages.First(stage => stage.StageType == StageType.InformationGovernance);
        var differentStage = project.PipelineStages.First(stage => stage.StageType == StageType.Prototype);
        var priorConversation = new Conversation(differentStage.Id, 5, _timeProvider);

        _projectRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(requestedStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(priorConversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priorConversation);

        var command = new CreateConversationCommand(requestedStage.Id, null, priorConversation.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("stage does not match", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ContinuationWithoutRequirementId_InheritsPriorRequirementId()
    {
        var project = CreateProjectWithStages();
        var requestedStage = project.PipelineStages.First(stage => stage.StageType == StageType.InformationGovernance);
        var priorConversation = new Conversation(requestedStage.Id, 5, _timeProvider, "REQ-007");

        _projectRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(requestedStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(priorConversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priorConversation);

        var command = new CreateConversationCommand(requestedStage.Id, null, priorConversation.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        _conversationRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Conversation>(conversation => conversation.RequirementId == "REQ-007"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ContinuationWithMismatchedRequirement_ThrowsInvalidOperationException()
    {
        var project = CreateProjectWithStages();
        var requestedStage = project.PipelineStages.First(stage => stage.StageType == StageType.InformationGovernance);
        var priorConversation = new Conversation(requestedStage.Id, 5, _timeProvider, "REQ-007");

        _projectRepositoryMock
            .Setup(repository => repository.GetByStageIdAsync(requestedStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(priorConversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priorConversation);

        var command = new CreateConversationCommand(requestedStage.Id, "REQ-123", priorConversation.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("requirement does not match", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ArchitectureBlockedWithPrototypeIncomplete_ThrowsInvalidOperationException()
    {
        var project = CreateProjectWithStages();
        var reqStage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);
        reqStage.Start(_timeProvider);
        reqStage.Complete("user-1", _timeProvider);
        // Prototype not complete yet → Architecture still Blocked

        var archStage = project.PipelineStages.First(s => s.StageType == StageType.Architecture);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(archStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CreateConversationCommand(archStage.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("blocked", exception.Message);
    }

    [Fact]
    public async Task Handle_ArchitectureWithPrototypeComplete_Succeeds()
    {
        var project = CreateProjectWithStages();
        var reqStage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);
        reqStage.Start(_timeProvider);
        reqStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var protoStage = project.PipelineStages.First(s => s.StageType == StageType.Prototype);
        protoStage.Start(_timeProvider);
        protoStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var archStage = project.PipelineStages.First(s => s.StageType == StageType.Architecture);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(archStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CreateConversationCommand(archStage.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.Equal(PipelineStageStatus.InProgress, archStage.Status);
    }

    [Fact]
    public async Task Handle_NormalisationBlockedWithClinicalSafetyIncomplete_ThrowsInvalidOperationException()
    {
        var project = CreateProjectWithStages();
        var reqStage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);
        reqStage.Start(_timeProvider);
        reqStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var protoStage = project.PipelineStages.First(s => s.StageType == StageType.Prototype);
        protoStage.Start(_timeProvider);
        protoStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        // Architecture, Design, PxD not complete → ClinicalSafety still Blocked → Normalisation still Blocked
        var normStage = project.PipelineStages.First(s => s.StageType == StageType.Normalisation);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(normStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CreateConversationCommand(normStage.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("blocked", exception.Message);
    }

    [Fact]
    public async Task Handle_NormalisationWithClinicalSafetyBlocked_Succeeds()
    {
        var project = CreateProjectWithStages(ComplianceDomain.Generic);
        var reqStage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);
        reqStage.Start(_timeProvider);
        reqStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var protoStage = project.PipelineStages.First(s => s.StageType == StageType.Prototype);
        protoStage.Start(_timeProvider);
        protoStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        // Complete Arch/Design/PxD to trigger Normalisation unblock (ClinicalSafety stays Blocked for Generic)
        var archStage = project.PipelineStages.First(s => s.StageType == StageType.Architecture);
        archStage.Start(_timeProvider);
        archStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var designStage = project.PipelineStages.First(s => s.StageType == StageType.Design);
        designStage.Start(_timeProvider);
        designStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var pxdStage = project.PipelineStages.First(s => s.StageType == StageType.Pxd);
        pxdStage.Start(_timeProvider);
        pxdStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var igStage = project.PipelineStages.First(s => s.StageType == StageType.InformationGovernance);
        igStage.Start(_timeProvider);
        igStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var secStage = project.PipelineStages.First(s => s.StageType == StageType.Security);
        secStage.Start(_timeProvider);
        secStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        // Normalisation should be unblocked now (ClinicalSafety is Blocked for Generic domain)
        var normStage = project.PipelineStages.First(s => s.StageType == StageType.Normalisation);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(normStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CreateConversationCommand(normStage.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task Handle_PlanningBlockedWithNormalisationIncomplete_ThrowsInvalidOperationException()
    {
        var project = CreateProjectWithStages(ComplianceDomain.Generic);
        var reqStage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);
        reqStage.Start(_timeProvider);
        reqStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var protoStage = project.PipelineStages.First(s => s.StageType == StageType.Prototype);
        protoStage.Start(_timeProvider);
        protoStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        // Planning still Blocked because Normalisation isn't complete
        var planStage = project.PipelineStages.First(s => s.StageType == StageType.Planning);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(planStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CreateConversationCommand(planStage.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("blocked", exception.Message);
    }

    [Fact]
    public async Task Handle_PlanningWithNormalisationComplete_Succeeds()
    {
        var project = CreateProjectWithStages(ComplianceDomain.Generic);
        var reqStage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);
        reqStage.Start(_timeProvider);
        reqStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var protoStage = project.PipelineStages.First(s => s.StageType == StageType.Prototype);
        protoStage.Start(_timeProvider);
        protoStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var archStage = project.PipelineStages.First(s => s.StageType == StageType.Architecture);
        archStage.Start(_timeProvider);
        archStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var designStage = project.PipelineStages.First(s => s.StageType == StageType.Design);
        designStage.Start(_timeProvider);
        designStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var pxdStage = project.PipelineStages.First(s => s.StageType == StageType.Pxd);
        pxdStage.Start(_timeProvider);
        pxdStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var normStage = project.PipelineStages.First(s => s.StageType == StageType.Normalisation);
        normStage.Start(_timeProvider);
        normStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var planStage = project.PipelineStages.First(s => s.StageType == StageType.Planning);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(planStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CreateConversationCommand(planStage.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.Equal(PipelineStageStatus.InProgress, planStage.Status);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ThrowsInvalidOperationException()
    {
        var stageId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new CreateConversationCommand(stageId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenValidStage_CreatesConversationWithCorrectTotalPhases()
    {
        var project = CreateProjectWithStages();
        var reqStage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(reqStage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _promptServiceMock
            .Setup(p => p.GetTotalPhases(StageType.RequirementsDiscovery))
            .Returns(7);

        var command = new CreateConversationCommand(reqStage.Id);

        await _handler.Handle(command, CancellationToken.None);

        _conversationRepositoryMock.Verify(
            r => r.AddAsync(It.Is<Conversation>(c => c.TotalPhases == 7), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
