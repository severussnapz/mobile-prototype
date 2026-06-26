using System.Security.Claims;
using System.Text;
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
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;

    public ArtefactVersionController(
        IMediator mediator,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        TimeProvider timeProvider)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
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
        if (string.IsNullOrWhiteSpace(filePath))
            return BadRequest("filePath query parameter is required.");

        var result = await _mediator.Send(new GetArtefactVersionsQuery(projectId, filePath), cancellationToken);
        if (result.Versions.Count == 0)
            return Ok(new List<ArtefactVersionResponse>());

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
        if (string.IsNullOrWhiteSpace(filePath))
            return BadRequest("filePath query parameter is required.");

        var result = await _mediator.Send(new GetArtefactVersionsQuery(projectId, filePath), cancellationToken);
        if (result.Versions.Count == 0)
            return Ok(new List<ArtefactVersionResponse>());

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
    [ProducesResponseType(typeof(ArtefactSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtefactSummaryResponse>> RestoreByBody(
        Guid projectId,
        [FromBody] RestoreArtefactVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Version <= 0)
            return BadRequest("version must be greater than 0.");

        return await RestoreArtefactVersionInternal(projectId, request.FilePath, request.Version, cancellationToken);
    }

    [HttpPost("versions/{version:int}/restore")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ArtefactSummaryResponse), StatusCodes.Status200OK)]
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

        return await RestoreArtefactVersionInternal(projectId, request.FilePath, version, cancellationToken);
    }

    private async Task<ActionResult<ArtefactSummaryResponse>> RestoreArtefactVersionInternal(
        Guid projectId,
        string filePath,
        int version,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return BadRequest("filePath is required.");

        var normalisedFilePath = filePath.Trim();
        var versions = await _artefactRepository.GetVersionsByFilePathAsync(projectId, normalisedFilePath, cancellationToken);
        if (versions.Count == 0)
            return NotFound();

        var sourceVersion = versions.FirstOrDefault(artefact => artefact.Version == version);
        if (sourceVersion is null)
            return NotFound();

        var latestVersion = versions.Max(artefact => artefact.Version);
        if (version == latestVersion)
        {
            return Ok(MapSummary(sourceVersion));
        }

        var sourceContent = await _artefactStorageService.GetContentAsync(sourceVersion.S3Key, cancellationToken);
        if (string.IsNullOrEmpty(sourceContent))
            return NotFound();

        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(projectId, normalisedFilePath, cancellationToken);
        var restoredContentType = sourceVersion.ContentType;

        var newStorageKey = await _artefactStorageService.SaveContentAsync(
            projectId,
            normalisedFilePath,
            nextVersion,
            sourceContent,
            restoredContentType,
            cancellationToken);

        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "system";
        var restoredArtefact = Artefact.CreateS3Artefact(
            projectId,
            nextVersion,
            normalisedFilePath,
            newStorageKey,
            restoredContentType,
            Encoding.UTF8.GetByteCount(sourceContent),
            userId,
            _timeProvider,
            true);

        await _artefactRepository.AddAsync(restoredArtefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapSummary(restoredArtefact));
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
            CreatedAt = artefact.CreatedAt
        };
    }
}
