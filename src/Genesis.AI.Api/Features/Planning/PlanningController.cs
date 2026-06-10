using System.Security.Claims;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.ApproveEmReview;
using Genesis.AI.Domain.Commands.RunPlanningPreflight;
using Genesis.AI.Domain.Commands.SplitPlanningTasks;
using Genesis.AI.Domain.Planning;
using Genesis.AI.Domain.Queries.GetPlanningArtefacts;
using Genesis.AI.Domain.Queries.GetPlanningStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Planning;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/planning")]
[Authorize(Policy = AuthorisationPolicies.ProjectRead)]
[Produces("application/json")]
public sealed class PlanningController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlanningController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost("run-preflight")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ApiResponse<PlanningRunActionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunPreflight(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "system";

        var result = await _mediator.Send(new RunPlanningPreflightCommand(projectId, userId), cancellationToken);

        if (result.Status == RunPlanningPreflightStatus.ProjectNotFound)
        {
            return NotFound(ApiErrorResponse.Create("404", "Project not found", result.ErrorDetail ?? "Project not found."));
        }

        return Ok(new ApiResponse<PlanningRunActionResponse>
        {
            Data = new PlanningRunActionResponse
            {
                PreflightPassed = result.PreflightPassed,
                Errors = result.Errors,
                OutputArtefacts = MapArtefacts(result.OutputArtefacts)
            }
        });
    }

    [HttpPost("approve-em-review")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ApiResponse<PlanningStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveEmReview(
        Guid projectId,
        [FromBody] ApproveEmReviewRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "system";

        var result = await _mediator.Send(
            new ApproveEmReviewCommand(projectId, userId, request?.Notes),
            cancellationToken);

        if (result.Status == ApproveEmReviewStatus.ProjectNotFound)
        {
            return NotFound(ApiErrorResponse.Create("404", "Project not found", result.ErrorDetail ?? "Project not found."));
        }

        if (result.Status == ApproveEmReviewStatus.TaskPlanMissing)
        {
            return Conflict(ApiErrorResponse.Create("409", "Task plan missing", result.ErrorDetail ?? "Task_Plan.md must exist before approving."));
        }

        if (result.Status == ApproveEmReviewStatus.TasksDataMissing)
        {
            return Conflict(ApiErrorResponse.Create("409", "Tasks data missing", result.ErrorDetail ?? "tasks_data.json must exist before approving."));
        }

        var statusResult = await _mediator.Send(new GetPlanningStatusQuery(projectId), cancellationToken);
        if (!statusResult.Found)
        {
            return NotFound(ApiErrorResponse.Create("404", "Project not found", "Project not found."));
        }

        return Ok(new ApiResponse<PlanningStatusResponse> { Data = MapStatus(statusResult) });
    }

    [HttpPost("split-tasks")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ApiResponse<PlanningSplitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SplitTasks(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "system";

        var result = await _mediator.Send(new SplitPlanningTasksCommand(projectId, userId), cancellationToken);

        if (result.Status == SplitPlanningTasksStatus.ProjectNotFound)
        {
            return NotFound(ApiErrorResponse.Create("404", "Project not found", result.ErrorDetail ?? "Project not found."));
        }

        if (result.Status is SplitPlanningTasksStatus.TasksDataMissing
            or SplitPlanningTasksStatus.EmApprovalMissing
            or SplitPlanningTasksStatus.EmApprovalStale
            or SplitPlanningTasksStatus.InvalidTasksData
            or SplitPlanningTasksStatus.DuplicateTaskIds
            or SplitPlanningTasksStatus.DuplicateCheckAssignments)
        {
            return Conflict(ApiErrorResponse.Create("409", "Split prerequisites not met", result.ErrorDetail ?? "Cannot split tasks."));
        }

        return Ok(new ApiResponse<PlanningSplitResponse>
        {
            Data = new PlanningSplitResponse
            {
                TaskCount = result.TaskCount,
                DuplicateTaskIds = result.DuplicateTaskIds,
                DuplicateCheckAssignments = result.DuplicateCheckAssignments,
                OutputArtefacts = MapArtefacts(result.OutputArtefacts)
            }
        });
    }

    [HttpGet("artefacts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PlanningArtefactResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArtefacts(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPlanningArtefactsQuery(projectId), cancellationToken);
        if (!result.Found)
        {
            return NotFound(ApiErrorResponse.Create("404", "Project not found", "Project not found."));
        }

        return Ok(new ApiResponse<IReadOnlyList<PlanningArtefactResponse>>
        {
            Data = MapArtefacts(result.Artefacts)
        });
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<PlanningStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPlanningStatusQuery(projectId), cancellationToken);
        if (!result.Found)
        {
            return NotFound(ApiErrorResponse.Create("404", "Project not found", "Project not found."));
        }

        return Ok(new ApiResponse<PlanningStatusResponse> { Data = MapStatus(result) });
    }

    private static PlanningStatusResponse MapStatus(GetPlanningStatusResult result)
    {
        return new PlanningStatusResponse
        {
            PreflightPassed = result.PreflightPassed,
            LastPreflightAtUtc = result.LastPreflightAtUtc,
            PreflightErrors = result.PreflightErrors,
            TaskPlanExists = result.TaskPlanExists,
            TasksDataExists = result.TasksDataExists,
            EmApproved = result.EmApproved,
            EmApprovalIsStale = result.EmApprovalIsStale,
            ApprovedBy = result.ApprovedBy,
            ApprovedAtUtc = result.ApprovedAtUtc,
            SplitPassed = result.SplitPassed,
            TaskCount = result.TaskCount,
            GatePassed = result.GatePassed,
            GateErrors = result.GateErrors,
            OutputArtefacts = MapArtefacts(result.OutputArtefacts)
        };
    }

    private static List<PlanningArtefactResponse> MapArtefacts(IReadOnlyList<PlanningArtefactSummary> artefacts)
    {
        return artefacts
            .Select(artefact => new PlanningArtefactResponse
            {
                ArtefactId = artefact.ArtefactId,
                FilePath = artefact.FilePath,
                Version = artefact.Version,
                UpdatedAt = artefact.UpdatedAt
            })
            .ToList();
    }
}
