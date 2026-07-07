using System.Security.Claims;
using AutoMapper;
using Genesis.AI.Api.Features.Conversations;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.CreateProject;
using Genesis.AI.Domain.Commands.DeleteProject;
using Genesis.AI.Domain.Commands.UpdateProjectDetails;
using Genesis.AI.Domain.Commands.UpdateProjectGitHub;
using Genesis.AI.Domain.Commands.UpdateProjectP00;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Queries.GetProjectById;
using Genesis.AI.Domain.Queries.GetProjectParkingLot;
using Genesis.AI.Domain.Queries.GetProjects;
using Genesis.AI.Domain.Queries.GetPushStatus;
using MediatR;
using Genesis.AI.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Projects;

[ApiController]
[Route("api/v1/projects")]
[Produces("application/json")]
[Consumes("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IMediator mediator,
        IMapper mapper,
        ILogger<ProjectsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new project with initialised pipeline stages.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ProjectResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ComplianceDomain>(
                request.ComplianceDomain,
                ignoreCase: true,
                out var complianceDomain))
        {
            return BadRequest(ApiErrorResponse.Create(
                "400",
                "Invalid compliance domain",
                $"'{request.ComplianceDomain}' is not a valid compliance domain. " +
                "Valid values are: ClinicalUk, Generic, Finance."));
        }

        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";

        var command = new CreateProjectCommand(
            request.Code,
            request.Name,
            request.Description,
            request.TimeSheetCode,
            complianceDomain,
            userId);

        Guid projectId;
        try
        {
            projectId = await _mediator.Send(command, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiErrorResponse.Create(
                "409",
                "Duplicate project code",
                ex.Message));
        }

        var project = await _mediator.Send(
            new GetProjectByIdQuery(projectId), cancellationToken);

        var resource = _mapper.Map<ProjectResource>(project);

        return CreatedAtAction(nameof(GetProject), new { id = projectId }, new ApiResponse<ProjectResource> { Data = resource });
    }

    /// <summary>
    /// Retrieves all projects, optionally filtered by status.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [ProducesResponseType(typeof(IEnumerable<ProjectResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjects(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = new GetProjectsQuery(status);
        var projects = await _mediator.Send(query, cancellationToken);
        var resources = _mapper.Map<IReadOnlyList<ProjectResource>>(projects);

        return Ok(new ApiResponse<IReadOnlyList<ProjectResource>> { Data = resources });
    }

    /// <summary>
    /// Retrieves a single project by its identifier, including pipeline stages.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [ProducesResponseType(typeof(ProjectResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProject(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProjectByIdQuery(id);
        var project = await _mediator.Send(query, cancellationToken);

        if (project is null)
        {
            return NotFound(ApiErrorResponse.Create(
                "404",
                "Project not found",
                $"No project found with ID '{id}'."));
        }

        var resource = _mapper.Map<ProjectResource>(project);
        return Ok(new ApiResponse<ProjectResource> { Data = resource });
    }

    /// <summary>
    /// Soft-deletes a project by its identifier.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
    {
        var project = await _mediator.Send(new GetProjectByIdQuery(id), cancellationToken);

        if (project is null)
        {
            return NotFound(ApiErrorResponse.Create(
                "404",
                "Project not found",
                $"No project found with ID '{id}'."));
        }

        await _mediator.Send(new DeleteProjectCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/details")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDetails(
        Guid id,
        [FromBody] UpdateProjectDetailsRequest request,
        CancellationToken ct)
    {
        var triggeredBy = User.GetEmail() ?? User.GetUserErn() ?? "unknown";

        var command = new UpdateProjectDetailsCommand(
            id,
            triggeredBy,
            request.Name,
            request.Description,
            request.TimeSheetCode,
            request.ComplianceDomain);

        try
        {
            await _mediator.Send(command, ct);
            return Ok();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { userMessage = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update project details {ProjectId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { userMessage = "Failed to save project details. Please try again." });
        }
    }

    [HttpPatch("{id:guid}/github")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGitHub(
        Guid id,
        [FromBody] UpdateProjectGitHubRequest request,
        CancellationToken ct)
    {
        var triggeredBy = User.GetEmail() ?? User.GetUserErn() ?? "unknown";

        var command = new UpdateProjectGitHubCommand(
            id,
            triggeredBy,
            request.GitHubApiRepoUrl,
            request.GitHubAppRepoUrl,
            request.FigmaFileUrl,
            request.FigmaPat);

        try
        {
            var result = await _mediator.Send(command, ct);
            return Ok(new { figmaPatPlaintext = result.FigmaPatPlaintext });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { userMessage = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update GitHub config {ProjectId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { userMessage = "Failed to save GitHub configuration. Please try again." });
        }
    }

    [HttpPatch("{id:guid}/p00")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateP00(
        Guid id,
        [FromBody] UpdateProjectP00Request request,
        CancellationToken ct)
    {
        var triggeredBy = User.GetEmail() ?? User.GetUserErn() ?? "unknown";

        var command = new UpdateProjectP00Command(
            id,
            triggeredBy,
            request.ReleaseType,
            request.AssuranceRequired,
            request.PilotDeploymentProcess,
            request.CsoRoleAssigned,
            request.IgOwnerRoleAssigned,
            request.SecurityReviewerAssigned,
            request.MedicalDeviceFlag);

        try
        {
            await _mediator.Send(command, ct);
            return Ok();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { userMessage = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update P00 config {ProjectId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { userMessage = "Failed to save project configuration. Please try again." });
        }
    }

    /// <summary>
    /// Get all parking lot items across all conversations in a project.
    /// </summary>
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [HttpGet("{id:guid}/parking-lot")]
    public async Task<IActionResult> GetProjectParkingLot(Guid id, CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new GetProjectParkingLotQuery(id), cancellationToken);
        if (items is null) return NotFound();

        var dtos = items.ToList().ConvertAll(item => new ParkingLotItemResponse
        {
            Id = item.Id,
            ConversationId = item.ConversationId,
            Content = item.Content,
            Priority = item.Priority.ToString().ToLowerInvariant(),
            Status = item.Status.ToString().ToLowerInvariant(),
            SourcePhase = item.SourcePhase,
            ResolvedAt = item.ResolvedAt,
            ClosureDecision = item.ClosureDecision,
            CreatedAt = item.CreatedAt
        });

        return Ok(new ApiResponse<List<ParkingLotItemResponse>> { Data = dtos });
    }

    /// <summary>
    /// Get aggregated token usage and estimated cost for all stages in a project.
    /// </summary>
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [HttpGet("{id:guid}/token-usage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjectTokenUsage(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new Domain.Queries.GetProjectTokenUsage.GetProjectTokenUsageQuery(id), cancellationToken);

        return Ok(new ApiResponse<ProjectTokenUsageResponse>
        {
            Data = new ProjectTokenUsageResponse
            {
                Stages = result.Stages,
                Totals = new TokenUsageTotals
                {
                    InputTokens = result.TotalInputTokens,
                    OutputTokens = result.TotalOutputTokens,
                    CacheReadInputTokens = result.TotalCacheReadInputTokens,
                    CacheWriteInputTokens = result.TotalCacheWriteInputTokens,
                    TurnCount = result.TotalTurnCount,
                    EstimatedCost = result.TotalEstimatedCost
                }
            }
        });
    }

    [HttpGet("{projectId:guid}/push-status")]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [ProducesResponseType(typeof(PushStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPushStatus(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPushStatusQuery(projectId), cancellationToken);
        return Ok(new PushStatusResponse(result.UnresolvedCount));
    }
}
