using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Moq;

namespace Genesis.AI.Tests.Services;

public class NormalisationGateServiceTests
{
    private static readonly string[] RequiredNormalisationFiles =
    [
        "checks.json",
        "hazards.json",
        "api_contracts.json",
        "schema.json",
        "interfaces.json",
        "components.json",
        "observability.json"
    ];

    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock;
    private readonly TimeProvider _timeProvider;
    private readonly NormalisationGateService _service;

    public NormalisationGateServiceTests()
    {
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        _timeProvider = TimeProvider.System;
        _service = new NormalisationGateService(_artefactRepositoryMock.Object, _artefactStorageServiceMock.Object);
    }

    private Artefact CreateArtefact(Guid projectId, string filePath, string? jsonContent)
    {
        var artefact = Artefact.CreateS3Artefact(
            projectId, 1, filePath, $"s3-{filePath}", "application/json", 10, "user-1", _timeProvider);

        if (jsonContent is not null)
        {
            _artefactStorageServiceMock
                .Setup(storage => storage.GetContentAsync(artefact.S3Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(jsonContent);
        }

        return artefact;
    }

    [Fact]
    public async Task EvaluateAsync_NoArtefacts_ReturnsGateFailedAndPrerequisitesNotMet()
    {
        var projectId = Guid.NewGuid();
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var evaluation = await _service.EvaluateAsync(projectId, "ACME", CancellationToken.None);

        Assert.False(evaluation.GatePassed);
        Assert.False(evaluation.RunPrerequisitesMet);
        Assert.NotEmpty(evaluation.Errors);
    }

    [Fact]
    public async Task EvaluateAsync_AllRequiredArtefactsPresent_ReturnsGatePassed()
    {
        var projectId = Guid.NewGuid();
        var artefacts = new List<Artefact>
        {
            CreateArtefact(projectId, "manifest.md", null),
            CreateArtefact(projectId, "requirements/REQ-001.md", null),
            CreateArtefact(projectId, "output/SECURITY_ASSURANCE_DATA.json", null),
            CreateArtefact(projectId, "output/SDP_EVIDENCE.json", null),
            CreateArtefact(projectId, "output/cross_cutting/traceability.json", "{}"),
            CreateArtefact(projectId, "output/cross_cutting/dependency_graph.json", "{}"),
            CreateArtefact(projectId, "output/cross_cutting/last_extracted.json", "{}"),
            CreateArtefact(projectId, "output/CS_Guardrails.json", "{}")
        };

        foreach (var requiredFile in RequiredNormalisationFiles)
        {
            artefacts.Add(CreateArtefact(projectId, $"output/REQ-001/{requiredFile}", "{}"));
        }

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artefacts);

        var evaluation = await _service.EvaluateAsync(projectId, "ACME", CancellationToken.None);

        Assert.True(evaluation.RunPrerequisitesMet);
        Assert.True(evaluation.GatePassed);
        Assert.Empty(evaluation.Errors);
    }

    [Fact]
    public async Task EvaluateAsync_RequirementOutputContainsInvalidJson_ReturnsGateFailed()
    {
        var projectId = Guid.NewGuid();
        var artefacts = new List<Artefact>
        {
            CreateArtefact(projectId, "manifest.md", null),
            CreateArtefact(projectId, "requirements/REQ-001.md", null),
            CreateArtefact(projectId, "output/SECURITY_ASSURANCE_DATA.json", null),
            CreateArtefact(projectId, "output/SDP_EVIDENCE.json", null),
            CreateArtefact(projectId, "output/cross_cutting/traceability.json", "{}"),
            CreateArtefact(projectId, "output/cross_cutting/dependency_graph.json", "{}"),
            CreateArtefact(projectId, "output/cross_cutting/last_extracted.json", "{}"),
            CreateArtefact(projectId, "output/CS_Guardrails.json", "{}")
        };

        foreach (var requiredFile in RequiredNormalisationFiles)
        {
            var content = requiredFile == "checks.json" ? "{not valid json" : "{}";
            artefacts.Add(CreateArtefact(projectId, $"output/REQ-001/{requiredFile}", content));
        }

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artefacts);

        var evaluation = await _service.EvaluateAsync(projectId, "ACME", CancellationToken.None);

        Assert.True(evaluation.RunPrerequisitesMet);
        Assert.False(evaluation.GatePassed);
        Assert.NotEmpty(evaluation.Errors);
    }
}
