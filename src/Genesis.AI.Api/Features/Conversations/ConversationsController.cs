using System.Security.Claims;
using AutoMapper;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.CreateConversation;
using Genesis.AI.Domain.Commands.SendMessage;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetConversation;
using Genesis.AI.Domain.Queries.GetConversationsByStage;
using MediatR;
using Genesis.AI.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Conversations;

[ApiController]
[Route("api/v1/conversations")]
[Produces("application/json")]
[Consumes("application/json")]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IConversationRepository _conversationRepository;

    public ConversationsController(IMediator mediator, IMapper mapper, IConversationRepository conversationRepository)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    /// <summary>
    /// Creates a new conversation for a pipeline stage.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorisationPolicies.ConversationWrite)]
    [ProducesResponseType(typeof(ConversationResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        // Runtime stage-type authorisation check
        var stageType = await _conversationRepository.GetStageTypeByStageIdAsync(request.StageId, cancellationToken);
        if (stageType is not null && !User.CanConverseOnStage(stageType.Value))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiErrorResponse.Create(
                "403",
                "Insufficient scope",
                $"You do not have permission to converse on {stageType.Value} stages."));
        }

        try
        {
            var command = new CreateConversationCommand(request.StageId);
            var conversationId = await _mediator.Send(command, cancellationToken);

            var conversation = await _mediator.Send(
                new GetConversationQuery(conversationId), cancellationToken);

            var resource = _mapper.Map<ConversationResource>(conversation);
            var projectContext = await _conversationRepository.GetProjectContextByStageIdAsync(request.StageId, cancellationToken);
            resource.ProjectId = projectContext?.ProjectId ?? Guid.Empty;

            return CreatedAtAction(nameof(GetConversation), new { id = conversationId }, new ApiResponse<ConversationResource> { Data = resource });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiErrorResponse.Create(
                "409",
                "Stage prerequisite not met",
                ex.Message));
        }
    }

    /// <summary>
    /// Gets a conversation by ID with all messages.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorisationPolicies.ConversationRead)]
    [ProducesResponseType(typeof(ConversationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _mediator.Send(
            new GetConversationQuery(id), cancellationToken);

        if (conversation is null)
        {
            return NotFound(ApiErrorResponse.Create(
                "404",
                "Conversation not found",
                $"No conversation found with ID '{id}'."));
        }

        var resource = _mapper.Map<ConversationResource>(conversation);
        var projectContext = await _conversationRepository.GetProjectContextByStageIdAsync(conversation.StageId, cancellationToken);
        resource.ProjectId = projectContext?.ProjectId ?? Guid.Empty;
        return Ok(new ApiResponse<ConversationResource> { Data = resource });
    }

    /// <summary>
    /// Gets all conversations for a pipeline stage.
    /// </summary>
    [HttpGet("by-stage/{stageId:guid}")]
    [Authorize(Policy = AuthorisationPolicies.ConversationRead)]
    [ProducesResponseType(typeof(IEnumerable<ConversationResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversationsByStage(Guid stageId, CancellationToken cancellationToken)
    {
        var conversations = await _mediator.Send(
            new GetConversationsByStageQuery(stageId), cancellationToken);

        var resources = _mapper.Map<IReadOnlyList<ConversationResource>>(conversations);
        var projectContext = await _conversationRepository.GetProjectContextByStageIdAsync(stageId, cancellationToken);
        var projectId = projectContext?.ProjectId ?? Guid.Empty;
        foreach (var resource in resources)
        {
            resource.ProjectId = projectId;
        }

        return Ok(new ApiResponse<IReadOnlyList<ConversationResource>> { Data = resources });
    }

    /// <summary>
    /// Sends a user message to a conversation. Returns the message ID.
    /// </summary>
    [HttpPost("{id:guid}/messages")]
    [Authorize(Policy = AuthorisationPolicies.ConversationWrite)]
    [ProducesResponseType(typeof(MessageResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(
        Guid id,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        // Runtime stage-type authorisation check
        var stageType = await _conversationRepository.GetStageTypeByConversationIdAsync(id, cancellationToken);
        if (stageType is not null && !User.CanConverseOnStage(stageType.Value))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiErrorResponse.Create(
                "403",
                "Insufficient scope",
                $"You do not have permission to converse on {stageType.Value} stages."));
        }

        var userId = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";
        var userErn = User.GetUserErn();
        var givenName = User.GetGivenName();
        var familyName = User.GetFamilyName();

        var command = new SendMessageCommand(id, request.Content, userId, userErn, givenName, familyName);
        var messageId = await _mediator.Send(command, cancellationToken);

        return Created($"/api/v1/conversations/{id}/messages/{messageId}", new ApiResponse<MessageCreatedResponse>
        {
            Data = new MessageCreatedResponse { Id = messageId }
        });
    }
}
