using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.VerifyCompleteness;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Normalisation;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class VerifyCompletenessCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<INormalisationGateService> _normalisationGateServiceMock;
    private readonly TimeProvider _timeProvider;
    private readonly VerifyCompletenessCommandHandler _handler;

    public VerifyCompletenessCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _normalisationGateServiceMock = new Mock<INormalisationGateService>();
        _timeProvider = TimeProvider.System;

        _handler = new VerifyCompletenessCommandHandler(
            _projectRepositoryMock.Object,
            _normalisationGateServiceMock.Object);
    }

    private Project CreateProject()
    {
        return new Project("ACME", "ACME Portal", null, "PORTASK0001045", ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsProjectNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await _handler.Handle(new VerifyCompletenessCommand(projectId), CancellationToken.None);

        Assert.Equal(VerifyCompletenessStatus.ProjectNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_GateFails_ReturnsErrors()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _normalisationGateServiceMock
            .Setup(service => service.EvaluateAsync(project.Id, project.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NormalisationGateEvaluation(
                RunPrerequisitesMet: true,
                GatePassed: false,
                Errors: ["Missing cross-cutting output file: output/cross_cutting/traceability.json"],
                OutputArtefacts: []));

        var result = await _handler.Handle(new VerifyCompletenessCommand(project.Id), CancellationToken.None);

        Assert.Equal(VerifyCompletenessStatus.Success, result.Status);
        Assert.False(result.GatePassed);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task Handle_GatePasses_ReturnsSuccess()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _normalisationGateServiceMock
            .Setup(service => service.EvaluateAsync(project.Id, project.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NormalisationGateEvaluation(
                RunPrerequisitesMet: true,
                GatePassed: true,
                Errors: [],
                OutputArtefacts: []));

        var result = await _handler.Handle(new VerifyCompletenessCommand(project.Id), CancellationToken.None);

        Assert.Equal(VerifyCompletenessStatus.Success, result.Status);
        Assert.True(result.GatePassed);
        Assert.Empty(result.Errors);
    }
}
