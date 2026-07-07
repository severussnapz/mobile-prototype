using System.Security.Claims;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.GenerateSessionClose;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.SessionClose;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/conversations/{conversationId:guid}")]
[Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
[Produces("application/json")]
public sealed class SessionCloseController : ControllerBase
{
    private readonly IMediator _mediator;

    public SessionCloseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("session-close")]
    [ProducesResponseType(typeof(ApiResponse<SessionCloseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SessionClose(
        Guid projectId,
        Guid conversationId,
        [FromQuery] StageType stageType,
        CancellationToken cancellationToken)
    {
        var userErn = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";
        var command = new GenerateSessionCloseCommand(projectId, conversationId, stageType, userErn);

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<SessionCloseResponse>
            {
                Data = new SessionCloseResponse(result.ArtefactId, result.FilePath, result.Version)
            });
        }
        catch (NotFoundException exception)
        {
            return NotFound(ApiErrorResponse.Create("404", "Conversation not found", exception.Message));
        }
    }
}