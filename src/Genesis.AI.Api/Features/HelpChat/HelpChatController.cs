using Genesis.AI.Api.Authentication;
using Genesis.AI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Tests.Api;

[ApiController]
[Route("api/v1/help")]
public sealed class HelpChatController : ControllerBase
{
    private readonly IHelpConversationRepository _helpConversationRepository;
    private readonly IHelpChatStreamService _helpChatStreamService;
    private readonly TimeProvider _timeProvider;

    public HelpChatController(
        IHelpConversationRepository helpConversationRepository,
        IHelpChatStreamService helpChatStreamService,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(helpConversationRepository);
        ArgumentNullException.ThrowIfNull(helpChatStreamService);

        _helpConversationRepository = helpConversationRepository;
        _helpChatStreamService = helpChatStreamService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    [HttpGet("conversations")]
    [Authorize(Policy = AuthorisationPolicies.ConversationRead)]
    public async Task<ActionResult<HelpConversationResponse?>> GetConversation(
        [FromQuery] Guid? projectId,
        CancellationToken cancellationToken)
    {
        var userErn = HttpContext?.User is { } principal
            ? principal.GetUserErn() ?? "anonymous"
            : string.Empty;
        var conversation = await _helpConversationRepository
            .GetMostRecentByUserAndProjectAsync(userErn, projectId, cancellationToken);

        if (conversation is null)
        {
            return new ActionResult<HelpConversationResponse?>(value: null);
        }

        return new HelpConversationResponse
        {
            Id = conversation.Id,
            ConversationId = conversation.Id
        };
    }

    [HttpPost("stream")]
    [Produces("text/event-stream")]
    [Authorize(Policy = AuthorisationPolicies.ConversationWrite)]
    public async Task<IActionResult> Stream(
        [FromBody] HelpStreamRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        _ = _timeProvider;

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var chunk in _helpChatStreamService.StreamAsync(request, cancellationToken))
        {
            var sseChunk = chunk.Replace("\n", "\\n");
            await Response.WriteAsync($"data: {sseChunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        return new EmptyResult();
    }
}
