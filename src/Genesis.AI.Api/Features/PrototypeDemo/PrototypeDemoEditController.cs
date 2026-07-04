using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.PrototypeDemo;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/prototype-demo")]
[Produces("application/json")]
[Consumes("application/json")]
public sealed class PrototypeDemoEditController : ControllerBase
{
    private readonly IPrototypeDemoEditService _editService;

    public PrototypeDemoEditController(IPrototypeDemoEditService editService)
    {
        _editService = editService ?? throw new ArgumentNullException(nameof(editService));
    }

    [HttpPost("edit")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ApiResponse<PrototypeElementEditResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EditElement(
        Guid projectId,
        [FromBody] EditPrototypeElementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _editService.EditElementAsync(
            projectId,
            new PrototypeElementEditRequest(
                request.SelectedOuterHtml,
                request.Instruction,
                request.ActiveUiKit,
                request.CurrentHtml,
                request.ConversationId),
            cancellationToken);

        return Ok(new ApiResponse<PrototypeElementEditResponse>
        {
            Data = new PrototypeElementEditResponse
            {
                Status = result.Status.ToString(),
                UpdatedOuterHtml = result.UpdatedOuterHtml,
                UpdatedFullHtml = result.UpdatedFullHtml,
                RejectionReason = result.RejectionReason
            }
        });
    }
}
