using System.Security.Claims;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.GenerateSecurityReviewReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.SecurityReviewReport;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/security-review-report")]
[Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
[Produces("application/json")]
public sealed class SecurityReviewReportController : ControllerBase
{
    private const string SpreadsheetContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IMediator _mediator;

    public SecurityReviewReportController(IMediator mediator)
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

        var result = await _mediator.Send(
            new GenerateSecurityReviewReportCommand(projectId, userId),
            cancellationToken);

        return result.Status switch
        {
            GenerateSecurityReviewReportStatus.ProjectNotFound => NotFound(ApiErrorResponse.Create(
                "404", "Project not found", result.ErrorDetail ?? "Project not found.")),
            GenerateSecurityReviewReportStatus.SecurityAssuranceDataNotFound => Conflict(ApiErrorResponse.Create(
                "409", "Security assurance data not available", result.ErrorDetail ?? "No security assurance data available.")),
            GenerateSecurityReviewReportStatus.SdpEvidenceNotFound => Conflict(ApiErrorResponse.Create(
                "409", "SDP evidence not available", result.ErrorDetail ?? "No SDP evidence available.")),
            GenerateSecurityReviewReportStatus.DataInvalid => Conflict(ApiErrorResponse.Create(
                "409", "Security data invalid", result.ErrorDetail ?? "Security data is invalid.")),
            _ => File(result.Content, SpreadsheetContentType, result.FileName)
        };
    }
}