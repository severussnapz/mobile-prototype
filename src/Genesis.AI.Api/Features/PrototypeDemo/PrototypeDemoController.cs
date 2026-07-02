using System.Security.Claims;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.GeneratePrototypeDemo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.PrototypeDemo;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/prototype-demo")]
[Produces("application/json")]
public sealed class PrototypeDemoController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrototypeDemoController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Generates a prototype-demo HTML page for a project and returns it as a
    /// self-contained HTML document. The stub implementation runs synchronously;
    /// the <c>IPrototypeDemoGenerationService</c> contract is
    /// <c>IAsyncEnumerable&lt;string&gt;</c> so a Day 2 SSE endpoint can forward
    /// the same stream without changing the service or handler.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [Produces("text/html")]
    [ProducesResponseType(typeof(ContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePrototypeDemo(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserErn()
            ?? User.FindFirstValue("sub")
            ?? "system";

        var result = await _mediator.Send(
            new GeneratePrototypeDemoCommand(projectId, userId), cancellationToken);

        return result.Status switch
        {
            GeneratePrototypeDemoStatus.ProjectNotFound => NotFound(ApiErrorResponse.Create(
                "404", "Project not found", result.ErrorDetail ?? "Project not found.")),
            _ => Content(result.Html, "text/html")
        };
    }
}
