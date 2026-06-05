using System.Security.Claims;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.GenerateHazardLog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.HazardLog;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/hazard-log")]
[Authorize(Policy = AuthorisationPolicies.ClinicalSafetyConverse)]
[Produces("application/json")]
public class HazardLogController : ControllerBase
{
    private const string SpreadsheetContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IMediator _mediator;

    public HazardLogController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Generates the clinical safety hazard log (IF678) spreadsheet for a project
    /// from its hazard registry and returns it as a downloadable <c>.xlsx</c> file.
    /// The generated log is also persisted as a versioned project artefact.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerateHazardLog(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "system";

        var result = await _mediator.Send(
            new GenerateHazardLogCommand(projectId, userId), cancellationToken);

        return result.Status switch
        {
            GenerateHazardLogStatus.ProjectNotFound => NotFound(ApiErrorResponse.Create(
                "404", "Project not found", result.ErrorDetail ?? "Project not found.")),
            GenerateHazardLogStatus.RegistryNotFound => Conflict(ApiErrorResponse.Create(
                "409", "Hazard registry not available", result.ErrorDetail ?? "No hazard registry available.")),
            _ => File(result.Content, SpreadsheetContentType, result.FileName)
        };
    }
}
