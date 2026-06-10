using System.Security.Claims;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.BypassNormalisationPlanningGate;
using Genesis.AI.Domain.Commands.RunLocalNormaliser;
using Genesis.AI.Domain.Commands.VerifyCompleteness;
using Genesis.AI.Domain.Normalisation;
using Genesis.AI.Domain.Queries.GetNormalisationArtefacts;
using Genesis.AI.Domain.Queries.GetNormalisationStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Normalisation;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/normalisation")]
[Authorize(Policy = AuthorisationPolicies.ProjectRead)]
[Produces("application/json")]
public sealed class NormalisationController : ControllerBase
{
    private readonly IMediator _mediator;

    public NormalisationController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost("extract-requirements")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ApiResponse<NormalisationRunActionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RunLocalNormaliser(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "system";

        var result = await _mediator.Send(
            new RunLocalNormaliserCommand(projectId, userId),
            cancellationToken);

        if (result.Status == RunLocalNormaliserStatus.ProjectNotFound)
        {
            return NotFound(ApiErrorResponse.Create(
                "404",
                "Project not found",
                result.ErrorDetail ?? "Project not found."));
        }

        if (result.Status == RunLocalNormaliserStatus.PrerequisitesMissing)
        {
            return Conflict(ApiErrorResponse.Create(
                "409",
                "Normalisation run prerequisites missing",
                result.ErrorDetail ?? "Normalisation run prerequisites are missing."));
        }

        return Ok(new ApiResponse<NormalisationRunActionResponse>
        {
            Data = new NormalisationRunActionResponse
            {
                RunStatus = result.RunStatus,
                GatePassed = result.GatePassed,
                Errors = result.Errors,
                OutputArtefacts = MapArtefacts(result.OutputArtefacts)
            }
        });
    }

    [HttpPost("verify-complete")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ApiResponse<NormalisationVerifyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyComplete(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new VerifyCompletenessCommand(projectId), cancellationToken);

        if (result.Status == VerifyCompletenessStatus.ProjectNotFound)
        {
            return NotFound(ApiErrorResponse.Create(
                "404",
                "Project not found",
                result.ErrorDetail ?? "Project not found."));
        }

        return Ok(new ApiResponse<NormalisationVerifyResponse>
        {
            Data = new NormalisationVerifyResponse
            {
                GatePassed = result.GatePassed,
                Errors = result.Errors,
                OutputArtefacts = MapArtefacts(result.OutputArtefacts)
            }
        });
    }

    [HttpPost("bypass-planning-gate")]
    [Authorize(Policy = AuthorisationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<NormalisationStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BypassPlanningGate(
        Guid projectId,
        [FromBody] BypassNormalisationPlanningGateRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "system";

        var result = await _mediator.Send(
            new BypassNormalisationPlanningGateCommand(projectId, userId, request?.Reason),
            cancellationToken);

        if (result.Status == BypassNormalisationPlanningGateStatus.ProjectNotFound)
        {
            return NotFound(ApiErrorResponse.Create(
                "404",
                "Project not found",
                result.ErrorDetail ?? "Project not found."));
        }

        var statusResult = await _mediator.Send(new GetNormalisationStatusQuery(projectId), cancellationToken);
        if (!statusResult.Found)
        {
            return NotFound(ApiErrorResponse.Create("404", "Project not found", "Project not found."));
        }

        return Ok(new ApiResponse<NormalisationStatusResponse>
        {
            Data = new NormalisationStatusResponse
            {
                RunStatus = statusResult.RunStatus,
                LastRunAtUtc = statusResult.LastRunAtUtc,
                RunErrors = statusResult.RunErrors,
                GatePassed = statusResult.GatePassed,
                PlanningEligible = statusResult.PlanningEligible,
                BypassActive = statusResult.BypassActive,
                BypassedBy = statusResult.BypassedBy,
                BypassedAtUtc = statusResult.BypassedAtUtc,
                GateErrors = statusResult.GateErrors,
                OutputArtefacts = MapArtefacts(statusResult.OutputArtefacts)
            }
        });
    }

    [HttpGet("artefacts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NormalisationArtefactResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGeneratedArtefacts(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetNormalisationArtefactsQuery(projectId), cancellationToken);
        if (!result.Found)
        {
            return NotFound(ApiErrorResponse.Create("404", "Project not found", "Project not found."));
        }

        return Ok(new ApiResponse<IReadOnlyList<NormalisationArtefactResponse>>
        {
            Data = MapArtefacts(result.Artefacts)
        });
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<NormalisationStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetNormalisationStatusQuery(projectId), cancellationToken);
        if (!result.Found)
        {
            return NotFound(ApiErrorResponse.Create("404", "Project not found", "Project not found."));
        }

        return Ok(new ApiResponse<NormalisationStatusResponse>
        {
            Data = new NormalisationStatusResponse
            {
                RunStatus = result.RunStatus,
                LastRunAtUtc = result.LastRunAtUtc,
                RunErrors = result.RunErrors,
                GatePassed = result.GatePassed,
                PlanningEligible = result.PlanningEligible,
                BypassActive = result.BypassActive,
                BypassedBy = result.BypassedBy,
                BypassedAtUtc = result.BypassedAtUtc,
                GateErrors = result.GateErrors,
                OutputArtefacts = MapArtefacts(result.OutputArtefacts)
            }
        });
    }

    private static List<NormalisationArtefactResponse> MapArtefacts(
        IReadOnlyList<NormalisationArtefactSummary> artefacts)
    {
        return artefacts
            .Select(artefact => new NormalisationArtefactResponse
            {
                ArtefactId = artefact.ArtefactId,
                FilePath = artefact.FilePath,
                Version = artefact.Version,
                UpdatedAt = artefact.UpdatedAt
            })
            .ToList();
    }
}
