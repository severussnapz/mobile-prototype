using System.Security.Claims;
using Genesis.AI.Domain.Commands.CreateArtefacts;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetArtefactById;
using Genesis.AI.Domain.Queries.GetArtefactsByStage;
using MediatR;
using Genesis.AI.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Artefacts;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/artefacts")]
[Produces("application/json")]
[Consumes("application/json")]
public class ArtefactController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IArtefactStorageService _artefactStorageService;

    public ArtefactController(
        IMediator mediator,
        IArtefactStorageService artefactStorageService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
    }

    /// <summary>
    /// Lists all artefacts for a project.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    public async Task<ActionResult<IReadOnlyList<ArtefactSummaryResponse>>> GetByProject(
        Guid projectId,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? prefix,
        CancellationToken cancellationToken)
    {
        var artefacts = await _mediator.Send(new GetArtefactsByStageQuery(projectId), cancellationToken);

        var dtos = artefacts.ToList().ConvertAll(artefact => new ArtefactSummaryResponse
        {
            Id = artefact.Id,
            ProjectId = artefact.ProjectId,
            Version = artefact.Version,
            FilePath = artefact.FilePath,
            ContentType = artefact.ContentType,
            SizeBytes = artefact.SizeBytes,
            CreatedBy = artefact.CreatedBy,
            CreatedAt = artefact.CreatedAt
        });

        if (!string.IsNullOrEmpty(prefix))
        {
            dtos = dtos
                .Where(a => a.FilePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Ok(dtos);
    }

    /// <summary>
    /// Gets a specific artefact's content.
    /// </summary>
    [HttpGet("{artefactId:guid}")]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    public async Task<ActionResult<ArtefactDetailResponse>> GetById(
        Guid projectId,
        Guid artefactId,
        CancellationToken cancellationToken)
    {
        var artefact = await _mediator.Send(new GetArtefactByIdQuery(artefactId), cancellationToken);
        if (artefact is null || artefact.ProjectId != projectId)
            return NotFound();

        var content = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);

        return Ok(new ArtefactDetailResponse
        {
            Id = artefact.Id,
            ProjectId = artefact.ProjectId,
            Version = artefact.Version,
            FilePath = artefact.FilePath,
            ContentType = artefact.ContentType,
            Content = content,
            SizeBytes = artefact.SizeBytes,
            CreatedBy = artefact.CreatedBy,
            CreatedAt = artefact.CreatedAt
        });
    }

    /// <summary>
    /// Downloads a specific artefact's raw bytes as a file attachment.
    /// Used for binary artefacts (spreadsheets, PDFs, images) that cannot be
    /// rendered as text in the browser.
    /// </summary>
    [HttpGet("{artefactId:guid}/download")]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [Produces("application/octet-stream")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid projectId,
        Guid artefactId,
        CancellationToken cancellationToken)
    {
        var artefact = await _mediator.Send(new GetArtefactByIdQuery(artefactId), cancellationToken);
        if (artefact is null || artefact.ProjectId != projectId)
            return NotFound();

        var content = await _artefactStorageService.GetBinaryContentAsync(artefact.S3Key, cancellationToken);
        if (content is null || content.Length == 0)
            return NotFound();

        var fileName = artefact.FilePath.TrimStart('/').Split('/').Last();

        return File(content, artefact.ContentType, fileName);
    }

    /// <summary>
    /// Saves one or more text artefacts for a project.
    /// Called by the frontend when the user confirms artefact output.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    public async Task<ActionResult<IReadOnlyList<ArtefactSummaryResponse>>> CreateArtefacts(
        Guid projectId,
        [FromBody] CreateArtefactsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Artefacts is null || request.Artefacts.Count == 0)
            return BadRequest("At least one artefact is required.");

        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "system";

        var command = new CreateArtefactsCommand(
            projectId,
            userId,
            request.Artefacts.ConvertAll(artefactRequest => new Domain.Commands.CreateArtefacts.CreateArtefactItem(
                artefactRequest.FilePath, artefactRequest.Content, artefactRequest.ContentType)));

        var artefacts = await _mediator.Send(command, cancellationToken);

        var dtos = artefacts.ToList().ConvertAll(artefact => new ArtefactSummaryResponse
        {
            Id = artefact.Id,
            ProjectId = artefact.ProjectId,
            Version = artefact.Version,
            FilePath = artefact.FilePath,
            ContentType = artefact.ContentType,
            SizeBytes = artefact.SizeBytes,
            CreatedBy = artefact.CreatedBy,
            CreatedAt = artefact.CreatedAt
        });

        return Created(string.Empty, dtos);
    }

}
