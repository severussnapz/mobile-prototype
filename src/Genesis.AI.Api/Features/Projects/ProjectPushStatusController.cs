using Genesis.AI.Api.Authentication;
using Genesis.AI.Domain.Queries.GetPushStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Projects;

[ApiController]
[Route("api/v1/projects")]
[Produces("application/json")]
public sealed class ProjectPushStatusController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectPushStatusController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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
