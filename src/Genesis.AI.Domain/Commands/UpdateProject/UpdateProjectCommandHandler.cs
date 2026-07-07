using Genesis.AI.Core.Data;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genesis.AI.Domain.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, UpdateProjectResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly IGenesisStructureScaffolder _genesisStructureScaffolder;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UpdateProjectCommandHandler> _logger;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        ISecretEncryptionService secretEncryptionService,
        IGenesisStructureScaffolder genesisStructureScaffolder,
        TimeProvider timeProvider)
        : this(
            projectRepository,
            secretEncryptionService,
            genesisStructureScaffolder,
            projectRepository?.UnitOfWork ?? throw new ArgumentNullException(nameof(projectRepository)),
            timeProvider,
            NullLogger<UpdateProjectCommandHandler>.Instance)
    {
    }

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        ISecretEncryptionService secretEncryptionService,
        IGenesisStructureScaffolder genesisStructureScaffolder,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<UpdateProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _secretEncryptionService = secretEncryptionService ?? throw new ArgumentNullException(nameof(secretEncryptionService));
        _genesisStructureScaffolder = genesisStructureScaffolder ?? throw new ArgumentNullException(nameof(genesisStructureScaffolder));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateProjectResult> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException($"Project with ID '{request.ProjectId}' was not found.");

        var wasGitHubConfigured = project.HasGitHubConfig;

        // Parse ComplianceDomain string to enum, fall back to existing value.
        var complianceDomain = project.ComplianceDomain;
        if (!string.IsNullOrWhiteSpace(request.ComplianceDomain)
            && Enum.TryParse<ComplianceDomain>(request.ComplianceDomain, ignoreCase: true, out var parsedDomain))
        {
            complianceDomain = parsedDomain;
        }

        // Always update details — fall back to existing values for omitted fields.
        project.UpdateDetails(
            !string.IsNullOrWhiteSpace(request.Name) ? request.Name : project.Name,
            request.Description ?? project.Description,
            !string.IsNullOrWhiteSpace(request.TimeSheetCode) ? request.TimeSheetCode : project.TimeSheetCode,
            complianceDomain,
            _timeProvider);

        // Update GitHub config if any GitHub field is provided and installation id is valid.
        var installationId = request.GitHubInstallationId ?? project.GitHubInstallationId;
        var gitHubRepoOwner = request.GitHubRepoOwner ?? project.GitHubRepoOwner;
        var gitHubRepoName = request.GitHubRepoName ?? project.GitHubRepoName;
        if (!string.IsNullOrWhiteSpace(installationId)
            && (request.GitHubApiRepoUrl is not null
                || request.GitHubAppRepoUrl is not null
                || request.GitHubInstallationId is not null))
        {
            project.SetGitHubConfig(
                request.GitHubApiRepoUrl ?? project.GitHubApiRepoUrl ?? string.Empty,
                request.GitHubAppRepoUrl ?? project.GitHubAppRepoUrl ?? string.Empty,
            gitHubRepoOwner ?? string.Empty,
            gitHubRepoName ?? string.Empty,
                installationId,
                _timeProvider);
        }

        string? encryptedPat = null;
        if (request.FigmaPat is not null)
        {
            encryptedPat = _secretEncryptionService.Encrypt(request.FigmaPat);
        }

        project.UpdateP00Configuration(
            request.ReleaseType,
            request.AssuranceRequired,
            request.PilotDeploymentProcess,
            request.CsoRoleAssigned,
            request.IgOwnerRoleAssigned,
            request.SecurityReviewerAssigned,
            request.MedicalDeviceFlag,
            request.FigmaFileUrl,
            encryptedPat,
            _timeProvider);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!wasGitHubConfigured && project.HasGitHubConfig)
        {
            _ = ScaffoldBestEffortAsync(project.Id, request.UpdatedBy, cancellationToken);
        }

        return new UpdateProjectResult(request.ProjectId, request.FigmaPat ?? null);
    }

    private async Task ScaffoldBestEffortAsync(Guid projectId, string userErn, CancellationToken ct)
    {
        try
        {
            await _genesisStructureScaffolder.ScaffoldAsync(projectId, userErn, ct);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Genesis structure scaffolding failed for project {ProjectId}.", projectId);
        }
    }
}
