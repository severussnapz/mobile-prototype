using AutoMapper;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.CreateNote;
using Genesis.AI.Domain.Commands.DeleteNote;
using Genesis.AI.Domain.Commands.UpdateNote;
using Genesis.AI.Domain.Queries.GetProjectNotes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Notes;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/notes")]
[Produces("application/json")]
[Consumes("application/json")]
public class NotesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public NotesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Lists all notes recorded against a project.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NoteResource>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotes(Guid projectId, CancellationToken cancellationToken)
    {
        var notes = await _mediator.Send(new GetProjectNotesQuery(projectId), cancellationToken);

        if (notes is null)
            return ProjectNotFound(projectId);

        var resources = _mapper.Map<IReadOnlyList<NoteResource>>(notes);
        return Ok(new ApiResponse<IReadOnlyList<NoteResource>> { Data = resources });
    }

    /// <summary>
    /// Creates a new note against a project.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ApiResponse<NoteResource>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateNote(
        Guid projectId,
        [FromBody] CreateNoteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateNoteCommand(
            projectId,
            request.Content,
            User.GetUserErn(),
            User.GetGivenName(),
            User.GetFamilyName());

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.ProjectFound)
            return ProjectNotFound(projectId);

        var resource = _mapper.Map<NoteResource>(result.Note);
        return CreatedAtAction(
            nameof(GetNotes),
            new { projectId },
            new ApiResponse<NoteResource> { Data = resource });
    }

    /// <summary>
    /// Updates the content of an existing note.
    /// </summary>
    [HttpPatch("{noteId:guid}")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ApiResponse<NoteResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNote(
        Guid projectId,
        Guid noteId,
        [FromBody] UpdateNoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateNoteCommand(projectId, noteId, request.Content), cancellationToken);

        if (!result.Found)
            return NoteNotFound(noteId);

        var resource = _mapper.Map<NoteResource>(result.Note);
        return Ok(new ApiResponse<NoteResource> { Data = resource });
    }

    /// <summary>
    /// Deletes a note.
    /// </summary>
    [HttpDelete("{noteId:guid}")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNote(
        Guid projectId,
        Guid noteId,
        CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(new DeleteNoteCommand(projectId, noteId), cancellationToken);

        if (!deleted)
            return NoteNotFound(noteId);

        return NoContent();
    }

    private NotFoundObjectResult ProjectNotFound(Guid projectId)
    {
        return NotFound(ApiErrorResponse.Create(
            "404",
            "Project not found",
            $"No project found with ID '{projectId}'."));
    }

    private NotFoundObjectResult NoteNotFound(Guid noteId)
    {
        return NotFound(ApiErrorResponse.Create(
            "404",
            "Note not found",
            $"No note found with ID '{noteId}'."));
    }
}
