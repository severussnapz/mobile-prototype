using Genesis.AI.Api.Authentication;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetArtefactById;
using Genesis.AI.Domain.Queries.GetArtefactsByStage;
using Genesis.AI.Domain.Queries.GetProjectById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Genesis.AI.Api.Features.Projects;

[ApiController]
[Route("api/v1/projects")]
[Produces("application/json")]
public sealed class ProjectGitHubController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProjectGitHubController> _logger;
    private readonly IGitHubArtefactPushService _githubPushService;
    private readonly IGenesisStructureScaffolder? _scaffolder;
    private readonly IServiceScopeFactory? _serviceScopeFactory;

    public ProjectGitHubController(
        IMediator mediator,
        ILogger<ProjectGitHubController> logger,
        IGitHubArtefactPushService githubPushService,
        IGenesisStructureScaffolder? scaffolder = null,
        IServiceScopeFactory? serviceScopeFactory = null)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _githubPushService = githubPushService ?? throw new ArgumentNullException(nameof(githubPushService));
        _scaffolder = scaffolder;
        _serviceScopeFactory = serviceScopeFactory;
    }

    [HttpPost("{id:guid}/push-all")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(PushActionResponse), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> PushAll(Guid id, CancellationToken ct)
    {
        var triggeredBy = User.GetEmail() ?? User.GetUserErn() ?? "unknown";
        if (_serviceScopeFactory is not null)
        {
            _ = PushAllBestEffortAsync(id, triggeredBy, _serviceScopeFactory);
        }

        return await Task.FromResult<IActionResult>(
            Accepted(new PushActionResponse("GitHub sync started. Check push status for results.")));
    }

    [HttpPost("{id:guid}/artefacts/{artefactId:guid}/push")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(PushActionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(PushActionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PushActionResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PushArtefact(Guid id, Guid artefactId, CancellationToken ct)
    {
        var triggeredBy = User.GetEmail() ?? User.GetUserErn() ?? "unknown";

        try
        {
            var artefact = await _mediator.Send(new GetArtefactByIdQuery(artefactId), ct);
            if (artefact is null || artefact.ProjectId != id)
            {
                return NotFound(new PushActionResponse("Artefact not found."));
            }

            var project = await _mediator.Send(new GetProjectByIdQuery(id), ct);
            if (project is not null && !project.HasGitHubConfig)
            {
                return BadRequest(new PushActionResponse(
                    "GitHub is not configured for this project. Go to Settings to add a GitHub repository."));
            }

            if (_scaffolder is not null)
            {
                try
                {
                    var scaffoldResult = await _scaffolder.ScaffoldAsync(id, triggeredBy, ct);
                    if (!scaffoldResult.IsSuccess)
                    {
                        _logger.LogWarning("Scaffold reported failure for project {ProjectId}: {FailureReason}", id, scaffoldResult.FailureReason);
                    }
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Scaffold failed {ProjectId}", id); }
            }

            await _githubPushService.PushAsync(
                id,
                artefactId,
                artefact.FilePath,
                artefact.Version,
                artefact.ContentType,
                artefact.S3Key,
                triggeredBy,
                ct);

            return Created(
                $"/api/v1/projects/{id}/artefacts/{artefactId}",
                new PushActionResponse("Pushed to GitHub successfully."));
        }
        catch (GitHubAuthenticationException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new PushActionResponse("Genesis AI cannot connect to GitHub. Check the installation ID in Project Settings."));
        }
        catch (GitHubFileTooLargeException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new PushActionResponse("This artefact is too large to push to GitHub (limit 12 MB)."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push failed for artefact {ArtefactId}", artefactId);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new PushActionResponse("Failed to push artefact to GitHub. Please try again."));
        }
    }

    private async Task PushAllBestEffortAsync(Guid projectId, string triggeredBy, IServiceScopeFactory scopeFactory)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var provider = scope.ServiceProvider;
            var pushService = provider.GetRequiredService<IGitHubArtefactPushService>();
            var mediator = provider.GetRequiredService<IMediator>();
            var logger = provider.GetRequiredService<ILogger<ProjectGitHubController>>();

            await ScaffoldForBulkPushAsync(provider, projectId, triggeredBy, logger);

            var artefacts = await mediator.Send(new GetArtefactsByStageQuery(projectId), CancellationToken.None);
            await PushArtefactsBestEffortAsync(pushService, artefacts, projectId, triggeredBy, logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk push failed for project {ProjectId}", projectId);
        }
    }

    private static async Task ScaffoldForBulkPushAsync(
        IServiceProvider provider, Guid projectId, string triggeredBy, ILogger logger)
    {
        var scaffolder = provider.GetService<IGenesisStructureScaffolder>();
        if (scaffolder is null)
        {
            return;
        }

        try
        {
            var scaffoldResult = await scaffolder.ScaffoldAsync(projectId, triggeredBy, CancellationToken.None);
            if (!scaffoldResult.IsSuccess)
            {
                logger.LogWarning("Scaffold reported failure for project {ProjectId}: {FailureReason}", projectId, scaffoldResult.FailureReason);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Scaffold failed {ProjectId}", projectId);
            var pushFailureLogRepo = provider.GetRequiredService<IPushFailureLogRepository>();
            var timeProvider = provider.GetRequiredService<TimeProvider>();
            var log = new PushFailureLog(projectId, Guid.Empty, ".genesis/scaffold", ex.Message, timeProvider);
            await pushFailureLogRepo.AddAsync(log, CancellationToken.None);
        }
    }

    private static async Task PushArtefactsBestEffortAsync(
        IGitHubArtefactPushService pushService,
        IReadOnlyList<Artefact> artefacts,
        Guid projectId,
        string triggeredBy,
        ILogger logger)
    {
        foreach (var artefact in artefacts)
        {
            try
            {
                await pushService.PushAsync(
                    projectId,
                    artefact.Id,
                    artefact.FilePath,
                    artefact.Version,
                    artefact.ContentType,
                    artefact.S3Key,
                    triggeredBy,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bulk push failed for artefact {ArtefactId}", artefact.Id);
            }
        }
    }
}
