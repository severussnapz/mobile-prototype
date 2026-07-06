using System.Security.Claims;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.SubmitMessageFeedback;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Conversations;

[ApiController]
[Route("api/v1/conversations/{conversationId:guid}/messages/{messageId:guid}/feedback")]
[Authorize(Policy = AuthorisationPolicies.ConversationWrite)]
[Produces("application/json")]
[Consumes("application/json")]
public class ConversationFeedbackController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationFeedbackController(
        IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MessageFeedbackResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitFeedback(
        Guid conversationId,
        Guid messageId,
        [FromBody] SubmitMessageFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var createdBy = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "unknown";
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        var command = new SubmitMessageFeedbackCommand(
            conversationId,
            messageId,
            request.IsHelpful,
            reason,
            createdBy);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            if (!result.ConversationExists)
            {
                return NotFound(ApiErrorResponse.Create(
                    "404",
                    "Conversation not found",
                    $"No conversation found with ID '{conversationId}'."));
            }

            return NotFound(ApiErrorResponse.Create(
                "404",
                "Assistant message not found",
                $"No assistant message found with ID '{messageId}' in conversation '{conversationId}'."));
        }

        var feedback = result.Feedback!;
        return Created($"/api/v1/conversations/{conversationId}/messages/{messageId}/feedback", new ApiResponse<MessageFeedbackResponse>
        {
            Data = new MessageFeedbackResponse
            {
                MessageId = feedback.MessageId,
                StageType = ToSnakeCase(feedback.StageType.ToString()),
                IsHelpful = feedback.IsHelpful,
                Reason = feedback.Reason,
                UpdatedAt = feedback.UpdatedAt
            }
        });
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var buffer = new System.Text.StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character))
            {
                buffer.Append('_');
            }

            buffer.Append(char.ToLowerInvariant(character));
        }

        return buffer.ToString();
    }
}