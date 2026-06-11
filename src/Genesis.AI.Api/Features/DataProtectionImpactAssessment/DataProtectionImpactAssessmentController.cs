using System.Security.Claims;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.GenerateDpiaReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.DataProtectionImpactAssessment;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/data-protection-impact-assessment")]
[Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
[Produces("application/json")]
public sealed class DataProtectionImpactAssessmentController : ControllerBase
{
    private const string WordContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private readonly IMediator _mediator;

    public DataProtectionImpactAssessmentController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Generate(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "system";

        var result = await _mediator.Send(new GenerateDpiaReportCommand(projectId, userId), cancellationToken);

        return result.Status switch
        {
            GenerateDpiaReportStatus.ProjectNotFound => NotFound(ApiErrorResponse.Create(
                "404", "Project not found", result.ErrorDetail ?? "Project not found.")),
            GenerateDpiaReportStatus.DataNotFound => Conflict(ApiErrorResponse.Create(
                "409", "DPIA data not available", result.ErrorDetail ?? "No DPIA data available.")),
            GenerateDpiaReportStatus.DataInvalid => Conflict(ApiErrorResponse.Create(
                "409", "DPIA data invalid", result.ErrorDetail ?? "DPIA data is invalid.")),
            _ => File(result.Content, WordContentType, result.FileName)
        };
    }
}
