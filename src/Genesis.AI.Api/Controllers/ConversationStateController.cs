using Genesis.AI.Api.Dtos;
using Genesis.AI.Domain.Commands.AddParkingLotItem;
using Genesis.AI.Domain.Commands.DeferParkingLotItem;
using Genesis.AI.Domain.Commands.DeleteParkingLotItem;
using Genesis.AI.Domain.Commands.AdvancePhase;
using Genesis.AI.Domain.Commands.ResolveParkingLotItem;
using Genesis.AI.Domain.Commands.SetPhase;
using Genesis.AI.Domain.Queries.GetConversationProgress;
using Genesis.AI.Domain.Queries.GetParkingLot;
using MediatR;
using Genesis.AI.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Controllers;

[ApiController]
[Route("api/v1/conversations/{conversationId:guid}")]
[Authorize(Policy = AuthorisationPolicies.ConversationWrite)]
[Produces("application/json")]
[Consumes("application/json")]
public class ConversationStateController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationStateController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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
            return BadRequest(new { errors = new[] { new { status = "400", detail = result.ValidationError } } });

        return Ok(new { data = new { phase = result.Phase, phaseName = result.PhaseName } });
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
            return BadRequest(new { errors = new[] { new { status = "400", detail = result.ValidationError } } });

        return Ok(new { data = new { phase = result.Phase, phaseName = result.PhaseName } });
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
            CreatedAt = item.CreatedAt
        });

        return Ok(new { data = dtos });
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
            return BadRequest(new { errors = new[] { new { status = "400", detail = result.ValidationError } } });

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
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResolveParkingLotItemCommand(conversationId, itemId), cancellationToken);
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
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeferParkingLotItemCommand(conversationId, itemId), cancellationToken);
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
