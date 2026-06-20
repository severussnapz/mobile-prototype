using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.AddParkingLotItem;
using Genesis.AI.Domain.Commands.DeferParkingLotItem;
using Genesis.AI.Domain.Commands.DeleteParkingLotItem;
using Genesis.AI.Domain.Commands.AdvancePhase;
using Genesis.AI.Domain.Commands.ReopenParkingLotItem;
using Genesis.AI.Domain.Commands.ResolveParkingLotItem;
using Genesis.AI.Domain.Commands.SetPhase;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetConversationProgress;
using Genesis.AI.Domain.Queries.GetParkingLot;
using MediatR;
using Genesis.AI.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Conversations;

[ApiController]
[Route("api/v1/conversations/{conversationId:guid}")]
[Authorize(Policy = AuthorisationPolicies.ConversationWrite)]
[Produces("application/json")]
[Consumes("application/json")]
public class ConversationStateController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConversationRepository _conversationRepository;
    private readonly IRequirementsFeedbackLoopService _requirementsFeedbackLoopService;

    public ConversationStateController(
        IMediator mediator,
        IConversationRepository conversationRepository,
        IRequirementsFeedbackLoopService requirementsFeedbackLoopService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _requirementsFeedbackLoopService = requirementsFeedbackLoopService ?? throw new ArgumentNullException(nameof(requirementsFeedbackLoopService));
    }

    // --- Phase Management ---

    /// <summary>
    /// Get current conversation progress (phase, questions asked, parking lot summary).
    /// </summary>
    [HttpGet("progress")]
    [Authorize(Policy = AuthorisationPolicies.ConversationRead)]
    public async Task<IActionResult> GetProgress(Guid conversationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetConversationProgressQuery(conversationId), cancellationToken);
        if (result is null) return NotFound();

        return Ok(new ConversationProgressResponse
        {
            CurrentPhase = result.CurrentPhase,
            PhaseName = result.PhaseName,
            TotalPhases = result.TotalPhases,
            QuestionsAsked = result.QuestionsAsked,
            EstimatedTotalQuestions = result.EstimatedTotalQuestions,
            PhaseNames = result.PhaseNames,
            Status = result.Status
        });
    }

    /// <summary>
    /// Advance conversation to the next phase.
    /// </summary>
    [HttpPost("advance-phase")]
    public async Task<IActionResult> AdvancePhase(Guid conversationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AdvancePhaseCommand(conversationId), cancellationToken);

        if (!result.Found) return NotFound();
        if (result.ValidationError is not null)
            return BadRequest(ApiErrorResponse.Create("400", result.ValidationError));

        return Ok(new ApiResponse<PhaseResponse>
        {
            Data = new PhaseResponse { Phase = result.Phase, PhaseName = result.PhaseName }
        });
    }

    /// <summary>
    /// Set conversation to a specific phase (for navigation).
    /// </summary>
    [HttpPatch("phase")]
    public async Task<IActionResult> SetPhase(Guid conversationId, [FromBody] SetPhaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetPhaseCommand(conversationId, request.Phase), cancellationToken);

        if (!result.Found) return NotFound();
        if (result.ValidationError is not null)
            return BadRequest(ApiErrorResponse.Create("400", result.ValidationError));

        return Ok(new ApiResponse<PhaseResponse>
        {
            Data = new PhaseResponse { Phase = result.Phase, PhaseName = result.PhaseName }
        });
    }

    /// <summary>
    /// Explicitly locks prototype edits for a requirement and appends substantive deltas to the requirement artefact.
    /// </summary>
    [HttpPost("prototype-lock")]
    public async Task<IActionResult> LockPrototype(
        Guid conversationId,
        [FromBody] LockPrototypeRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            return NotFound();
        }

        var stageType = await _conversationRepository.GetStageTypeByStageIdAsync(conversation.StageId, cancellationToken);
        if (stageType != Domain.Enums.StageType.Prototype)
        {
            return BadRequest(ApiErrorResponse.Create("400", "Prototype lock is only valid for Prototype stage conversations."));
        }

        var projectContext = await _conversationRepository.GetProjectContextByStageIdAsync(conversation.StageId, cancellationToken);
        if (projectContext is null)
        {
            return NotFound();
        }

        var requirementId = string.IsNullOrWhiteSpace(request.RequirementId)
            ? conversation.RequirementId
            : request.RequirementId.Trim();

        if (string.IsNullOrWhiteSpace(requirementId))
        {
            return BadRequest(ApiErrorResponse.Create("400", "RequirementId is required for prototype lock."));
        }

        var requirementFilePath = string.IsNullOrWhiteSpace(request.RequirementFilePath)
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"requirements/{requirementId}.md")
            : request.RequirementFilePath.Trim();

        var lockedBy = User.GetUserErn() ?? User.FindFirst("sub")?.Value ?? "system";

        var result = await _requirementsFeedbackLoopService.LockPrototypeAsync(
            projectContext.ProjectId,
            requirementId,
            requirementFilePath,
            lockedBy,
            cancellationToken);

        return Ok(new ApiResponse<PrototypeLockResponse>
        {
            Data = new PrototypeLockResponse
            {
                Success = result.Success,
                Message = result.Message,
                AppendedDeltaCount = result.AppendedDeltaCount,
                LockedAt = result.LockedAt,
                LockBatchId = result.LockBatchId
            }
        });
    }

    // --- Parking Lot ---

    /// <summary>
    /// Get all parking lot items for a conversation.
    /// </summary>
    [HttpGet("parking-lot")]
    [Authorize(Policy = AuthorisationPolicies.ConversationRead)]
    public async Task<IActionResult> GetParkingLot(Guid conversationId, CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new GetParkingLotQuery(conversationId), cancellationToken);
        if (items is null) return NotFound();

        var dtos = items.ToList().ConvertAll(item => new ParkingLotItemResponse
        {
            Id = item.Id,
            ConversationId = item.ConversationId,
            Content = item.Content,
            Priority = item.Priority.ToString().ToLowerInvariant(),
            Status = item.Status.ToString().ToLowerInvariant(),
            SourcePhase = item.SourcePhase,
            ResolvedAt = item.ResolvedAt,
            ClosureDecision = item.ClosureDecision,
            CreatedAt = item.CreatedAt
        });

        return Ok(new ApiResponse<List<ParkingLotItemResponse>> { Data = dtos });
    }

    /// <summary>
    /// Add a new item to the parking lot.
    /// </summary>
    [HttpPost("parking-lot")]
    public async Task<IActionResult> AddParkingLotItem(
        Guid conversationId,
        [FromBody] CreateParkingLotItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddParkingLotItemCommand(conversationId, request.Content, request.Priority), cancellationToken);

        if (!result.Found) return NotFound();
        if (result.ValidationError is not null)
            return BadRequest(ApiErrorResponse.Create("400", result.ValidationError));

        var item = result.Item!;
        return Created($"api/v1/conversations/{conversationId}/parking-lot/{item.Id}", new ParkingLotItemResponse
        {
            Id = item.Id,
            ConversationId = conversationId,
            Content = item.Content,
            Priority = item.Priority.ToString().ToLowerInvariant(),
            Status = item.Status.ToString().ToLowerInvariant(),
            SourcePhase = item.SourcePhase,
            ResolvedAt = item.ResolvedAt,
            ClosureDecision = item.ClosureDecision,
            CreatedAt = item.CreatedAt
        });
    }

    /// <summary>
    /// Resolve a parking lot item.
    /// </summary>
    [HttpPost("parking-lot/{itemId:guid}/resolve")]
    public async Task<IActionResult> ResolveParkingLotItem(
        Guid conversationId,
        Guid itemId,
        [FromBody] UpdateParkingLotItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Decision))
        {
            return BadRequest(ApiErrorResponse.Create("400", "Decision is required when resolving a parking lot item."));
        }

        var result = await _mediator.Send(
            new ResolveParkingLotItemCommand(conversationId, itemId, request.Decision.Trim()),
            cancellationToken);
        if (!result.Found) return NotFound();

        var item = result.Item!;
        return Ok(new ParkingLotItemResponse
        {
            Id = item.Id,
            ConversationId = conversationId,
            Content = item.Content,
            Priority = item.Priority.ToString().ToLowerInvariant(),
            Status = item.Status.ToString().ToLowerInvariant(),
            SourcePhase = item.SourcePhase,
            ResolvedAt = item.ResolvedAt,
            ClosureDecision = item.ClosureDecision,
            CreatedAt = item.CreatedAt
        });
    }

    /// <summary>
    /// Defer a parking lot item.
    /// </summary>
    [HttpPost("parking-lot/{itemId:guid}/defer")]
    public async Task<IActionResult> DeferParkingLotItem(
        Guid conversationId,
        Guid itemId,
        [FromBody] UpdateParkingLotItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Decision))
        {
            return BadRequest(ApiErrorResponse.Create("400", "Decision is required when deferring a parking lot item."));
        }

        var result = await _mediator.Send(
            new DeferParkingLotItemCommand(conversationId, itemId, request.Decision.Trim()),
            cancellationToken);
        if (!result.Found) return NotFound();

        var item = result.Item!;
        return Ok(new ParkingLotItemResponse
        {
            Id = item.Id,
            ConversationId = conversationId,
            Content = item.Content,
            Priority = item.Priority.ToString().ToLowerInvariant(),
            Status = item.Status.ToString().ToLowerInvariant(),
            SourcePhase = item.SourcePhase,
            ResolvedAt = item.ResolvedAt,
            ClosureDecision = item.ClosureDecision,
            CreatedAt = item.CreatedAt
        });
    }

    /// <summary>
    /// Reopen a resolved or deferred parking lot item.
    /// </summary>
    [HttpPost("parking-lot/{itemId:guid}/reopen")]
    public async Task<IActionResult> ReopenParkingLotItem(
        Guid conversationId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ReopenParkingLotItemCommand(conversationId, itemId), cancellationToken);
        if (!result.Found) return NotFound();

        var item = result.Item!;
        return Ok(new ParkingLotItemResponse
        {
            Id = item.Id,
            ConversationId = conversationId,
            Content = item.Content,
            Priority = item.Priority.ToString().ToLowerInvariant(),
            Status = item.Status.ToString().ToLowerInvariant(),
            SourcePhase = item.SourcePhase,
            ResolvedAt = item.ResolvedAt,
            ClosureDecision = item.ClosureDecision,
            CreatedAt = item.CreatedAt
        });
    }

    /// <summary>
    /// Delete a parking lot item.
    /// </summary>
    [HttpDelete("parking-lot/{itemId:guid}")]
    public async Task<IActionResult> DeleteParkingLotItem(
        Guid conversationId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var found = await _mediator.Send(new DeleteParkingLotItemCommand(conversationId, itemId), cancellationToken);
        if (!found) return NotFound();

        return NoContent();
    }
}
