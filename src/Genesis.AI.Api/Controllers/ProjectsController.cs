using System.Security.Claims;
using AutoMapper;
using Genesis.AI.Api.Dtos;
using Genesis.AI.Api.Requests;
using Genesis.AI.Api.Resources;
using Genesis.AI.Domain.Commands.CreateProject;
using Genesis.AI.Domain.Commands.DeleteProject;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Queries.GetProjectById;
using Genesis.AI.Domain.Queries.GetProjectParkingLot;
using Genesis.AI.Domain.Queries.GetProjects;
using MediatR;
using Genesis.AI.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Produces("application/json")]
[Consumes("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ProjectsController(
        IMediator mediator,
        IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Creates a new project with initialised pipeline stages.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ProjectResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ComplianceDomain>(
                request.ComplianceDomain,
                ignoreCase: true,
                out var complianceDomain))
        {
            return BadRequest(new
            {
                errors = new[]
                {
                    new
                    {
                        status = "400",
                        title = "Invalid compliance domain",
                        detail = $"'{request.ComplianceDomain}' is not a valid compliance domain. " +
                                 "Valid values are: ClinicalUk, Generic, Finance."
                    }
                }
            });
        }

        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";

        var command = new CreateProjectCommand(
            request.Code,
            request.Name,
            request.Description,
            complianceDomain,
            userId);

        var projectId = await _mediator.Send(command, cancellationToken);

        var project = await _mediator.Send(
            new GetProjectByIdQuery(projectId), cancellationToken);

        var resource = _mapper.Map<ProjectResource>(project);

        return CreatedAtAction(nameof(GetProject), new { id = projectId }, new { data = resource });
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

        return Ok(new { data = resources });
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
            return NotFound(new
            {
                errors = new[]
                {
                    new
                    {
                        status = "404",
                        title = "Project not found",
                        detail = $"No project found with ID '{id}'."
                    }
                }
            });
        }

        var resource = _mapper.Map<ProjectResource>(project);
        return Ok(new { data = resource });
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
            return NotFound(new
            {
                errors = new[]
                {
                    new
                    {
                        status = "404",
                        title = "Project not found",
                        detail = $"No project found with ID '{id}'."
                    }
                }
            });
        }

        await _mediator.Send(new DeleteProjectCommand(id), cancellationToken);
        return NoContent();
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
            CreatedAt = item.CreatedAt
        });

        return Ok(new { data = dtos });
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

        return Ok(new
        {
            data = new
            {
                stages = result.Stages,
                totals = new
                {
                    inputTokens = result.TotalInputTokens,
                    outputTokens = result.TotalOutputTokens,
                    cacheReadInputTokens = result.TotalCacheReadInputTokens,
                    cacheWriteInputTokens = result.TotalCacheWriteInputTokens,
                    turnCount = result.TotalTurnCount,
                    estimatedCost = result.TotalEstimatedCost
                }
            }
        });
    }
}
