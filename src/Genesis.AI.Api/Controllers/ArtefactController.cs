using System.Security.Claims;
using Genesis.AI.Api.Dtos;
using Genesis.AI.Domain.Commands.CreateArtefacts;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetArtefactById;
using Genesis.AI.Domain.Queries.GetArtefactsByStage;
using MediatR;
using Genesis.AI.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/artefacts")]
[Produces("application/json")]
[Consumes("application/json")]
public class ArtefactController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IArtefactStorageService _artefactStorageService;

    public ArtefactController(IMediator mediator, IArtefactStorageService artefactStorageService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
    }

    /// <summary>
    /// Lists all artefacts for a project.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    public async Task<ActionResult<IReadOnlyList<ArtefactDto>>> GetByProject(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var artefacts = await _mediator.Send(new GetArtefactsByStageQuery(projectId), cancellationToken);

        var dtos = artefacts.ToList().ConvertAll(artefact => new ArtefactDto
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

        return Ok(dtos);
    }

    /// <summary>
    /// Gets a specific artefact's content.
    /// </summary>
    [HttpGet("{artefactId:guid}")]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    public async Task<ActionResult<ArtefactDetailDto>> GetById(
        Guid projectId,
        Guid artefactId,
        CancellationToken cancellationToken)
    {
        var artefact = await _mediator.Send(new GetArtefactByIdQuery(artefactId), cancellationToken);
        if (artefact is null || artefact.ProjectId != projectId)
            return NotFound();

        var content = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);

        return Ok(new ArtefactDetailDto
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
    /// Saves one or more text artefacts for a project.
    /// Called by the frontend when the user confirms artefact output.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    public async Task<ActionResult<IReadOnlyList<ArtefactDto>>> CreateArtefacts(
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

        var dtos = artefacts.ToList().ConvertAll(artefact => new ArtefactDto
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
