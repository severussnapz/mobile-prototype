using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.GenerateSecurityReviewReport;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.SecurityReviewReport;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class GenerateSecurityReviewReportCommandHandlerTests
{
    private const string SecurityAssuranceDataPath = "output/SECURITY_ASSURANCE_DATA.json";
    private const string SdpEvidencePath = "output/SDP_EVIDENCE.json";

    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _storageServiceMock;
    private readonly Mock<ISecurityReviewReportBuilder> _reportBuilderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly GenerateSecurityReviewReportCommandHandler _handler;

    public GenerateSecurityReviewReportCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _storageServiceMock = new Mock<IArtefactStorageService>();
        _reportBuilderMock = new Mock<ISecurityReviewReportBuilder>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _artefactRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new GenerateSecurityReviewReportCommandHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _storageServiceMock.Object,
            _reportBuilderMock.Object,
            _timeProvider);
    }

    private Project CreateProject()
    {
        return new Project("ACME", "ACME Portal", null, "TS-001", ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
    }

    private Artefact CreateSecurityAssuranceArtefact(Guid projectId)
    {
        return Artefact.CreateS3Artefact(
            projectId,
            1,
            SecurityAssuranceDataPath,
            "s3-security-assurance-data-key",
            "application/json",
            100,
            "user-1",
            _timeProvider, true);
    }

    private Artefact CreateSdpEvidenceArtefact(Guid projectId)
    {
        return Artefact.CreateS3Artefact(
            projectId,
            1,
            SdpEvidencePath,
            "s3-sdp-evidence-key",
            "application/json",
            100,
            "user-1",
            _timeProvider, true);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsProjectNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await _handler.Handle(new GenerateSecurityReviewReportCommand(projectId, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateSecurityReviewReportStatus.ProjectNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_SecurityAssuranceDataMissing_ReturnsDataNotFound()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id,
                SecurityAssuranceDataPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var result = await _handler.Handle(new GenerateSecurityReviewReportCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateSecurityReviewReportStatus.SecurityAssuranceDataNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_SdpEvidenceMissing_ReturnsDataNotFound()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id,
                SecurityAssuranceDataPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSecurityAssuranceArtefact(project.Id));
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id,
                SdpEvidencePath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var result = await _handler.Handle(new GenerateSecurityReviewReportCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateSecurityReviewReportStatus.SdpEvidenceNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_InvalidSourceData_ReturnsDataInvalid()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id,
                SecurityAssuranceDataPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSecurityAssuranceArtefact(project.Id));
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id,
                SdpEvidencePath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSdpEvidenceArtefact(project.Id));
        _storageServiceMock
            .Setup(storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{}");
        _reportBuilderMock
            .Setup(builder => builder.Build(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("bad payload"));

        var result = await _handler.Handle(new GenerateSecurityReviewReportCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateSecurityReviewReportStatus.DataInvalid, result.Status);
    }

    [Fact]
    public async Task Handle_ValidData_ReturnsSuccessAndPersistsWorkbook()
    {
        var project = CreateProject();
        var content = new byte[] { 35, 32, 83, 101, 99, 117, 114, 105, 116, 121 };

        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id,
                SecurityAssuranceDataPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSecurityAssuranceArtefact(project.Id));
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id,
                SdpEvidencePath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSdpEvidenceArtefact(project.Id));
        _storageServiceMock
            .SetupSequence(storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"document_version\":\"1\",\"project\":{\"name\":\"Project\",\"summary\":\"Summary\",\"architecture_context\":\"Context\"},\"threat_model\":{\"assets\":[\"A\"],\"actors\":[\"B\"],\"entry_points\":[\"C\"],\"abuse_cases\":[\"D\"]},\"attack_vector_coverage\":{\"repo_secrets\":{\"status\":\"covered\",\"controls\":[\"C1\"],\"evidence_refs\":[\"E1\"]},\"ci_cd_exposure\":{\"status\":\"covered\",\"controls\":[\"C1\"],\"evidence_refs\":[\"E1\"]},\"supply_chain\":{\"status\":\"covered\",\"controls\":[\"C1\"],\"evidence_refs\":[\"E1\"]},\"injection\":{\"status\":\"covered\",\"controls\":[\"C1\"],\"evidence_refs\":[\"E1\"]},\"authn_authz\":{\"status\":\"covered\",\"controls\":[\"C1\"],\"evidence_refs\":[\"E1\"]},\"crypto\":{\"status\":\"covered\",\"controls\":[\"C1\"],\"evidence_refs\":[\"E1\"]},\"logging_monitoring\":{\"status\":\"covered\",\"controls\":[\"C1\"],\"evidence_refs\":[\"E1\"]}},\"control_mappings\":[{\"control_id\":\"SEC-001\",\"title\":\"Secure by default\",\"owasp\":[\"A01\"],\"asvs\":[\"1.1.1\"],\"cwe\":[\"CWE-1\"],\"internal_policy_refs\":[\"IP123\"],\"applicability_rationale\":\"Rationale\",\"requirement_ids\":[\"REQ-1\"]}],\"checks\":[{\"check_id\":\"CHECK-1\",\"control_id\":\"SEC-001\",\"test_type\":\"positive\",\"scenario\":\"Scenario\",\"pass_criteria\":\"Criteria\",\"evidence_ref\":\"E1\"}],\"evidence_artifacts\":[{\"artifact_id\":\"ART-1\",\"type\":\"policy\",\"location\":\"/tmp\",\"description\":\"Desc\"}],\"review_signoff\":{\"reviewer\":\"Lead\",\"role\":\"security lead\",\"decision\":\"approved\",\"reference\":\"REF\",\"date\":\"2026-06-09\"}}")
            .ReturnsAsync("{\"project_code\":\"ACME\",\"generated_at\":\"2026-06-09T00:00:00Z\",\"controls\":[{\"control_id\":\"SDP-001\",\"name\":\"Control\",\"status\":\"pass\",\"evidence_type\":\"policy_doc\",\"evidence_ref\":\"REF\",\"owner\":\"Team\",\"last_reviewed\":\"2026-06-09\"}]}" );
        _reportBuilderMock
            .Setup(builder => builder.Build(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(content);
        _storageServiceMock
            .Setup(storage => storage.SaveBinaryContentAsync(
                project.Id,
                It.IsAny<string>(),
                1,
                content,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-security-report-key");

        Artefact? savedArtefact = null;
        _artefactRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Callback<Artefact, CancellationToken>((artefact, _) => savedArtefact = artefact)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new GenerateSecurityReviewReportCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateSecurityReviewReportStatus.Success, result.Status);
        Assert.NotNull(savedArtefact);
        Assert.Equal("feedback/SECURITY_REVIEW_REPORT.xlsx", savedArtefact!.FilePath);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", savedArtefact.ContentType);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}