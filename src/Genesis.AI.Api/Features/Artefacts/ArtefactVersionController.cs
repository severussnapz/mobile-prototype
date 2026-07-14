using System.Security.Claims;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetArtefactVersions;
using MediatR;
using Genesis.AI.Api.Authentication;
using JsonApi.Resources.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Artefacts;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/artefacts")]
[Produces("application/json")]
[Consumes("application/json")]
public class ArtefactVersionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IArtefactRestorationService _restorationService;

    public ArtefactVersionController(
        IMediator mediator,
        IArtefactRestorationService restorationService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _restorationService = restorationService ?? throw new ArgumentNullException(nameof(restorationService));
    }

    [HttpGet("versions")]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ArtefactVersionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ArtefactVersionResponse>>> GetVersionsByFilePath(
        Guid projectId,
        [FromQuery] string filePath,
        [FromQuery] PaginationFilter pagination,
        CancellationToken cancellationToken)
    {
        return await GetVersionsInternal(projectId, filePath, pagination, cancellationToken);
    }

    [HttpGet("history")]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ArtefactVersionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ArtefactVersionResponse>>> GetHistoryByFilePath(
        Guid projectId,
        [FromQuery] string filePath,
        [FromQuery] PaginationFilter pagination,
        CancellationToken cancellationToken)
    {
        return await GetVersionsInternal(projectId, filePath, pagination, cancellationToken);
    }

    private async Task<ActionResult<IReadOnlyList<ArtefactVersionResponse>>> GetVersionsInternal(
        Guid projectId,
        string filePath,
        PaginationFilter pagination,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return BadRequest("filePath query parameter is required.");

        var result = await _mediator.Send(new GetArtefactVersionsQuery(projectId, filePath), cancellationToken);
        
        // For prototype files, always use S3 history as the source of truth
        if (string.Equals(filePath.Trim(), "prototype/index.html", StringComparison.OrdinalIgnoreCase))
        {
            var fallbackVersions = await _restorationService.BuildS3FallbackVersionsAsync(
                projectId, filePath, pagination, cancellationToken);
            return Ok(fallbackVersions);
        }

        // For non-prototype files, use DB results or S3 fallback if DB is empty
        if (result.Versions.Count == 0)
        {
            var fallbackVersions = await _restorationService.BuildS3FallbackVersionsAsync(
                projectId, filePath, pagination, cancellationToken);
            return Ok(fallbackVersions);
        }

        var page = Math.Max(1, pagination.Page);
        var size = Math.Max(1, pagination.Size);
        var skip = (page - 1) * size;

        var versions = result.Versions
            .Skip(skip)
            .Take(size)
            .ToList()
            .ConvertAll(artefact => new ArtefactVersionResponse
            {
                Id = artefact.Id,
                Version = artefact.Version,
                CreatedAt = artefact.CreatedAt,
                CreatedBy = artefact.CreatedBy,
                SizeBytes = artefact.SizeBytes,
                ContentType = artefact.ContentType
            });

        return Ok(versions);
    }

    [HttpPost("restore")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ArtefactSummaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtefactSummaryResponse>> RestoreByBody(
        Guid projectId,
        [FromBody] RestoreArtefactVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Version <= 0)
            return BadRequest("version must be greater than 0.");

        var restored = await _restorationService.RestoreArtefactVersionAsync(
            projectId, request.FilePath, request.Version, 
            User.GetUserErn() ?? User.FindFirstValue("sub"), 
            cancellationToken);
        
        if (restored is null)
            return NotFound();

        var summary = MapSummary(restored);
        return CreatedAtAction(nameof(GetVersionsByFilePath), 
            new { projectId, filePath = restored.FilePath }, summary);
    }

    [HttpPost("versions/{version:int}/restore")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ArtefactSummaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtefactSummaryResponse>> RestoreByRoute(
        Guid projectId,
        int version,
        [FromBody] RestoreArtefactVersionRouteRequest request,
        CancellationToken cancellationToken)
    {
        if (version <= 0)
            return BadRequest("version must be greater than 0.");

        var restored = await _restorationService.RestoreArtefactVersionAsync(
            projectId, request.FilePath, version, 
            User.GetUserErn() ?? User.FindFirstValue("sub"), 
            cancellationToken);
        
        if (restored is null)
            return NotFound();

        var summary = MapSummary(restored);
        return CreatedAtAction(nameof(GetVersionsByFilePath), 
            new { projectId, filePath = restored.FilePath }, summary);
    }

    private static ArtefactSummaryResponse MapSummary(Artefact artefact)
    {
        return new ArtefactSummaryResponse
        {
            Id = artefact.Id,
            ProjectId = artefact.ProjectId,
            Version = artefact.Version,
            FilePath = artefact.FilePath,
            ContentType = artefact.ContentType,
            SizeBytes = artefact.SizeBytes,
            CreatedBy = artefact.CreatedBy,
            CreatedAt = artefact.CreatedAt,
            GitHubPushedAt = artefact.GitHubPushedAt
        };
    }
}
