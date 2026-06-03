using System.Security.Claims;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.CompleteStage;
using Genesis.AI.Domain.Commands.SkipStage;
using MediatR;
using Genesis.AI.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Stages;

[ApiController]
[Route("api/v1/stages")]
[Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
[Produces("application/json")]
[Consumes("application/json")]
public class PipelineStagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PipelineStagesController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Marks a pipeline stage as complete. Validates that at least one artefact exists.
    /// </summary>
    [HttpPost("{stageId:guid}/complete")]
    public async Task<IActionResult> CompleteStage(Guid stageId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "system";
        var result = await _mediator.Send(new CompleteStageCommand(stageId, userId), cancellationToken);

        if (!result.Found) return NotFound();
        if (result.AlreadyComplete)
            return Ok(new ApiResponse<StageMessageResponse>
            {
                Data = new StageMessageResponse { StageId = stageId, Message = "Stage is already complete." }
            });
        if (result.ValidationError is not null)
            return BadRequest(ApiErrorResponse.Create("400", result.ValidationError));

        return Ok(new ApiResponse<StageStatusResponse>
        {
            Data = new StageStatusResponse { StageId = result.StageId, StageType = result.StageType, Status = result.Status }
        });
    }

    /// <summary>
    /// Skips a pipeline stage (e.g. Prototype for non-VS-Code workflows).
    /// </summary>
    [HttpPost("{stageId:guid}/skip")]
    public async Task<IActionResult> SkipStage(Guid stageId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SkipStageCommand(stageId), cancellationToken);

        if (!result.Found) return NotFound();
        if (result.ValidationError is not null)
            return BadRequest(ApiErrorResponse.Create("400", result.ValidationError));

        return Ok(new ApiResponse<StageStatusResponse>
        {
            Data = new StageStatusResponse { StageId = result.StageId, StageType = result.StageType, Status = result.Status }
        });
    }
}
