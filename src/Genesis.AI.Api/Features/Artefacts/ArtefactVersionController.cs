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
    // The single-file prototype artefact is the only file whose historical versions are
    // recovered directly from S3 when the database no longer tracks them. Hardcoded intentionally.
    private const string PrototypeHtmlArtefactPath = "prototype/index.html";

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
            return Ok(await BuildS3FallbackVersionsAsync(projectId, filePath, pagination, cancellationToken));

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
            return Ok(await BuildS3FallbackVersionsAsync(projectId, filePath, pagination, cancellationToken));

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

        var sourceVersion = versions.FirstOrDefault(artefact => artefact.Version == version);
        if (sourceVersion is null)
            return await RestorePrototypeVersionFromS3Async(projectId, normalisedFilePath, version, cancellationToken);

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

    private async Task<ActionResult<ArtefactSummaryResponse>> RestorePrototypeVersionFromS3Async(
        Guid projectId,
        string normalisedFilePath,
        int version,
        CancellationToken cancellationToken)
    {
        // S3 is the source of truth for prototype history when the database no longer tracks the version.
        if (!string.Equals(normalisedFilePath, PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var s3Key = $"projects/{projectId}/artefacts/{PrototypeHtmlArtefactPath}/v{version}";
        var sourceContent = await _artefactStorageService.GetContentAsync(s3Key, cancellationToken);
        if (string.IsNullOrEmpty(sourceContent))
            return NotFound();

        const string restoredContentType = "text/html";
        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(projectId, normalisedFilePath, cancellationToken);

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

    private async Task<List<ArtefactVersionResponse>> BuildS3FallbackVersionsAsync(
        Guid projectId,
        string filePath,
        PaginationFilter pagination,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(filePath.Trim(), PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase))
            return [];

        var s3Versions = await _artefactStorageService.ListVersionsAsync(projectId, PrototypeHtmlArtefactPath, cancellationToken);
        if (s3Versions.Count == 0)
            return [];

        var page = Math.Max(1, pagination.Page);
        var size = Math.Max(1, pagination.Size);
        var skip = (page - 1) * size;

        return s3Versions
            .Skip(skip)
            .Take(size)
            .Select(entry => new ArtefactVersionResponse
            {
                Id = Guid.Empty,
                Version = entry.Version,
                CreatedAt = entry.LastModified,
                CreatedBy = "system",
                SizeBytes = entry.SizeBytes,
                ContentType = "text/html"
            })
            .ToList();
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
