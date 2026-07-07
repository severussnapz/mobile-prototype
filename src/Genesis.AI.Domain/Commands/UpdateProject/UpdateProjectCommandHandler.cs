using Genesis.AI.Core.Data;
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

        if (request.GitHubInstallationId is not null)
        {
            project.SetGitHubConfig(
                request.GitHubApiRepoUrl ?? string.Empty,
                request.GitHubAppRepoUrl ?? string.Empty,
                request.GitHubRepoOwner ?? string.Empty,
                request.GitHubRepoName ?? string.Empty,
                request.GitHubInstallationId,
                _timeProvider);
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
            null,
            _timeProvider);

        if (request.FigmaPat is not null)
        {
            var encryptedPat = _secretEncryptionService.Encrypt(request.FigmaPat);
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
        }

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
