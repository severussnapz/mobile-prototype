using System.Security.Claims;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Commands.ApproveRequirementChange;
using Genesis.AI.Domain.Commands.ProposeRequirementChange;
using Genesis.AI.Domain.Commands.RecordDomainReview;
using Genesis.AI.Domain.Commands.RejectRequirementChange;
using Genesis.AI.Domain.Commands.UndoApproveRequirementChange;
using Genesis.AI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.RequirementChanges;

// TODO(plan4): wrap command handlers in MediatR IRequest<T> — see Option B in RequirementChangesController design notes
[ApiController]
[Route("api/v1/projects/{projectId:guid}/requirement-changes")]
[Authorize(Policy = AuthorisationPolicies.ConversationWrite)]
[Produces("application/json")]
[Consumes("application/json")]
public sealed class RequirementChangesController : ControllerBase
{
    private readonly IRequirementChangeRepository _repository;
    private readonly ProposeRequirementChangeCommandHandler _proposeHandler;
    private readonly ApproveRequirementChangeCommandHandler _approveHandler;
    private readonly UndoApproveRequirementChangeCommandHandler _undoHandler;
    private readonly RejectRequirementChangeCommandHandler _rejectHandler;
    private readonly RecordDomainReviewCommandHandler _reviewHandler;
    private readonly IChangeFileWriterService _changeFileWriterService;

    public RequirementChangesController(
        IRequirementChangeRepository repository,
        ProposeRequirementChangeCommandHandler proposeHandler,
        ApproveRequirementChangeCommandHandler approveHandler,
        UndoApproveRequirementChangeCommandHandler undoHandler,
        RejectRequirementChangeCommandHandler rejectHandler,
        RecordDomainReviewCommandHandler reviewHandler,
        IChangeFileWriterService changeFileWriterService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _proposeHandler = proposeHandler ?? throw new ArgumentNullException(nameof(proposeHandler));
        _approveHandler = approveHandler ?? throw new ArgumentNullException(nameof(approveHandler));
        _undoHandler = undoHandler ?? throw new ArgumentNullException(nameof(undoHandler));
        _rejectHandler = rejectHandler ?? throw new ArgumentNullException(nameof(rejectHandler));
        _reviewHandler = reviewHandler ?? throw new ArgumentNullException(nameof(reviewHandler));
        _changeFileWriterService = changeFileWriterService ?? throw new ArgumentNullException(nameof(changeFileWriterService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RequirementChangeResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListChanges(
        Guid projectId,
        [FromQuery] bool pendingOnly = false,
        CancellationToken cancellationToken = default)
    {
        var changes = pendingOnly
            ? await _repository.GetPendingByProjectIdAsync(projectId, cancellationToken)
            : await _repository.GetByProjectIdAsync(projectId, cancellationToken);

        var response = changes.Select(RequirementChangeResponse.FromDomain).ToList();
        return Ok(new ApiResponse<IReadOnlyList<RequirementChangeResponse>> { Data = response });
    }

    [HttpGet("{changeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RequirementChangeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChange(
        Guid projectId,
        Guid changeId,
        CancellationToken cancellationToken)
    {
        var change = await _repository.GetByIdAsync(changeId, cancellationToken);
        if (change is null || change.ProjectId != projectId)
        {
            return NotFound(ApiErrorResponse.Create("404", "Not found",
                $"Requirement change '{changeId}' not found."));
        }

        return Ok(new ApiResponse<RequirementChangeResponse>
        {
            Data = RequirementChangeResponse.FromDomain(change)
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RequirementChangeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Propose(
        Guid projectId,
        [FromBody] ProposeRequirementChangeRequest request,
        CancellationToken cancellationToken)
    {
        var createdBy = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";

        if (!TryParseChangeType(request.ChangeType, out var changeType))
        {
            return BadRequest(ApiErrorResponse.Create("400", "Invalid change_type",
                $"change_type must be one of: gap, clarification, contradiction"));
        }

        var raisingPipeline = HttpContext.Request.Headers["X-Pipeline-Stage"].FirstOrDefault()
            ?? "unknown";

        var command = new ProposeRequirementChangeCommand(
            ProjectId: projectId,
            ReqId: request.ReqId,
            ChangeType: changeType,
            RaisingPipeline: raisingPipeline,
            RaisingPipelineConversationId: request.RaisingPipelineConversationId,
            ProposedAcText: request.ProposedAcText,
            Rationale: request.Rationale,
            CreatedBy: createdBy);

        var result = await _proposeHandler.Handle(command, cancellationToken);

        var change = await _repository.GetByIdAsync(result.ChangeId, cancellationToken);
        return CreatedAtAction(nameof(GetChange),
            new { projectId, changeId = result.ChangeId },
            new ApiResponse<RequirementChangeResponse>
            {
                Data = RequirementChangeResponse.FromDomain(change!)
            });
    }

    [HttpPost("{changeId:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<RequirementChangeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(
        Guid projectId,
        Guid changeId,
        [FromBody] ApproveRequirementChangeRequest request,
        CancellationToken cancellationToken)
    {
        var approvedBy = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";

        var command = new ApproveRequirementChangeCommand(
            ChangeId: changeId,
            ApprovedAcText: request.ApprovedAcText,
            ClinicalSafetyImpact: ParseImpact(request.ClinicalSafetyImpact),
            IgImpact: ParseImpact(request.IgImpact),
            SecurityImpact: ParseImpact(request.SecurityImpact),
            ApprovedBy: approvedBy);

        await _approveHandler.Handle(command, cancellationToken);

        var change = await _repository.GetByIdAsync(changeId, cancellationToken);
        await _changeFileWriterService.WriteChangeFileAsync(change!, cancellationToken);

        return Ok(new ApiResponse<RequirementChangeResponse>
        {
            Data = RequirementChangeResponse.FromDomain(change!)
        });
    }

    [HttpPost("{changeId:guid}/undo")]
    [ProducesResponseType(typeof(ApiResponse<RequirementChangeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Undo(
        Guid projectId,
        Guid changeId,
        [FromBody] UndoRequirementChangeRequest request,
        CancellationToken cancellationToken)
    {
        var undoneBy = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";

        var command = new UndoApproveRequirementChangeCommand(
            ChangeId: changeId,
            UndoneBy: undoneBy,
            UndoRationale: request.UndoRationale);

        await _undoHandler.Handle(command, cancellationToken);

        var change = await _repository.GetByIdAsync(changeId, cancellationToken);
        await _changeFileWriterService.WriteChangeFileAsync(change!, cancellationToken);

        return Ok(new ApiResponse<RequirementChangeResponse>
        {
            Data = RequirementChangeResponse.FromDomain(change!)
        });
    }

    [HttpPost("{changeId:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<RequirementChangeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(
        Guid projectId,
        Guid changeId,
        CancellationToken cancellationToken)
    {
        var rejectedBy = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";

        await _rejectHandler.Handle(
            new RejectRequirementChangeCommand(changeId, rejectedBy),
            cancellationToken);

        var change = await _repository.GetByIdAsync(changeId, cancellationToken);
        return Ok(new ApiResponse<RequirementChangeResponse>
        {
            Data = RequirementChangeResponse.FromDomain(change!)
        });
    }

    [HttpPost("{changeId:guid}/clinical-safety-review")]
    [ProducesResponseType(typeof(ApiResponse<RequirementChangeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClinicalSafetyReview(
        Guid projectId,
        Guid changeId,
        CancellationToken cancellationToken)
    {
        var reviewer = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";

        await _reviewHandler.Handle(
            new RecordDomainReviewCommand(changeId, ReviewDomain.ClinicalSafety, reviewer),
            cancellationToken);

        var change = await _repository.GetByIdAsync(changeId, cancellationToken);
        await _changeFileWriterService.WriteChangeFileAsync(change!, cancellationToken);

        return Ok(new ApiResponse<RequirementChangeResponse>
        {
            Data = RequirementChangeResponse.FromDomain(change!)
        });
    }

    [HttpPost("{changeId:guid}/ig-review")]
    [ProducesResponseType(typeof(ApiResponse<RequirementChangeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> IgReview(
        Guid projectId,
        Guid changeId,
        CancellationToken cancellationToken)
    {
        var reviewer = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";

        await _reviewHandler.Handle(
            new RecordDomainReviewCommand(changeId, ReviewDomain.InformationGovernance, reviewer),
            cancellationToken);

        var change = await _repository.GetByIdAsync(changeId, cancellationToken);
        await _changeFileWriterService.WriteChangeFileAsync(change!, cancellationToken);

        return Ok(new ApiResponse<RequirementChangeResponse>
        {
            Data = RequirementChangeResponse.FromDomain(change!)
        });
    }

    [HttpPost("{changeId:guid}/security-review")]
    [ProducesResponseType(typeof(ApiResponse<RequirementChangeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SecurityReview(
        Guid projectId,
        Guid changeId,
        CancellationToken cancellationToken)
    {
        var reviewer = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";

        await _reviewHandler.Handle(
            new RecordDomainReviewCommand(changeId, ReviewDomain.Security, reviewer),
            cancellationToken);

        var change = await _repository.GetByIdAsync(changeId, cancellationToken);
        await _changeFileWriterService.WriteChangeFileAsync(change!, cancellationToken);

        return Ok(new ApiResponse<RequirementChangeResponse>
        {
            Data = RequirementChangeResponse.FromDomain(change!)
        });
    }

    private static bool TryParseChangeType(string value, out ChangeType result)
    {
        result = value.ToLowerInvariant() switch
        {
            "gap" => ChangeType.Gap,
            "clarification" => ChangeType.Clarification,
            "contradiction" => ChangeType.Contradiction,
            _ => default
        };

        return value.ToLowerInvariant() is "gap" or "clarification" or "contradiction";
    }

    private static ImpactLevel ParseImpact(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "possible" => ImpactLevel.Possible,
            "definite" => ImpactLevel.Definite,
            _ => ImpactLevel.None
        };
    }
}
