using Genesis.AI.Api.Authentication;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetArtefactById;
using Genesis.AI.Domain.Queries.GetArtefactsByStage;
using Genesis.AI.Domain.Queries.GetProjectById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public ProjectGitHubController(
        IMediator mediator,
        ILogger<ProjectGitHubController> logger,
        IGitHubArtefactPushService githubPushService,
        IGenesisStructureScaffolder? scaffolder = null)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _githubPushService = githubPushService ?? throw new ArgumentNullException(nameof(githubPushService));
        _scaffolder = scaffolder;
    }

    [HttpPost("{id:guid}/push-all")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(PushActionResponse), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> PushAll(Guid id, CancellationToken ct)
    {
        var triggeredBy = User.GetEmail() ?? User.GetUserErn() ?? "unknown";
        _ = PushAllBestEffortAsync(id, triggeredBy, ct);

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
                    await _scaffolder.ScaffoldAsync(id, triggeredBy, ct);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Scaffold failed {ProjectId}", id);
                }
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

    private async Task PushAllBestEffortAsync(Guid projectId, string triggeredBy, CancellationToken ct)
    {
        try
        {
            if (_scaffolder is not null)
            {
                try
                {
                    await _scaffolder.ScaffoldAsync(projectId, triggeredBy, ct);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Scaffold failed {ProjectId}", projectId);
                }
            }

            var artefacts = await _mediator.Send(new GetArtefactsByStageQuery(projectId), ct);
            foreach (var artefact in artefacts)
            {
                try
                {
                    await _githubPushService.PushAsync(
                        projectId,
                        artefact.Id,
                        artefact.FilePath,
                        artefact.Version,
                        artefact.ContentType,
                        artefact.S3Key,
                        triggeredBy,
                        ct);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Bulk push failed for artefact {ArtefactId}", artefact.Id);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Bulk push failed for project {ProjectId}", projectId);
        }
    }
}