using System.Security.Claims;
using System.Text;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.GeneratePrototypeDemo;
using Genesis.AI.Domain.Commands.SavePrototypeDemoHtml;
using Genesis.AI.Domain.Queries.GetPrototypeDemoHtml;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.PrototypeDemo;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/prototype-demo")]
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
    [Produces("text/html", "application/json")]
    [ProducesResponseType(typeof(ContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status504GatewayTimeout)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
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
            GeneratePrototypeDemoStatus.TimedOut => StatusCode(StatusCodes.Status504GatewayTimeout, ApiErrorResponse.Create(
                "504", "Prototype generation timed out", result.ErrorDetail ?? "Prototype generation timed out.")),
            GeneratePrototypeDemoStatus.GenerationFailed => StatusCode(StatusCodes.Status500InternalServerError, ApiErrorResponse.Create(
                "500", "Prototype generation failed", result.ErrorDetail ?? "Prototype generation failed.")),
            _ => Content(result.Html, "text/html")
        };
    }

    /// <summary>
    /// Persists the current prototype-demo HTML document as a versioned project artefact
    /// (<c>prototype-demo/index.html</c>) so it survives a page refresh. The raw HTML is
    /// read directly from the request body — no <c>text/html</c> input formatter is
    /// registered. Re-saving bumps the version of the single artefact row in place.
    /// </summary>
    [HttpPost("save")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [Consumes("text/html")]
    [Produces("text/html", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SavePrototypeDemoHtml(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserErn()
            ?? User.FindFirstValue("sub")
            ?? "system";

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var html = await reader.ReadToEndAsync(cancellationToken);

        var result = await _mediator.Send(
            new SavePrototypeDemoHtmlCommand(projectId, html, userId), cancellationToken);

        return result.Status switch
        {
            SavePrototypeDemoHtmlStatus.ProjectNotFound => NotFound(ApiErrorResponse.Create(
                "404", "Project not found", result.ErrorDetail ?? "Project not found.")),
            _ => Content(result.ArtefactId.ToString(), "text/html")
        };
    }

    /// <summary>
    /// Returns the saved prototype-demo HTML document (<c>prototype-demo/index.html</c>)
    /// for a project, or 404 if none has been saved yet.
    /// </summary>
    [HttpGet("html")]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [Produces("text/html", "application/json")]
    [ProducesResponseType(typeof(ContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrototypeDemoHtml(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPrototypeDemoHtmlQuery(projectId), cancellationToken);

        return result.Status switch
        {
            GetPrototypeDemoHtmlStatus.NotFound => NotFound(ApiErrorResponse.Create(
                "404", "Prototype not found", "No saved prototype demo exists for this project.")),
            _ => Content(result.Html, "text/html")
        };
    }
}
