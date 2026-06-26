using Genesis.AI.Api.Authentication;
using Genesis.AI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Prototypes;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/prototypes/structural-edits")]
[Produces("application/json")]
[Consumes("application/json")]
public sealed class PrototypeStructuralEditsController : ControllerBase
{
    private readonly IStructuralEditService _structuralEditService;

    public PrototypeStructuralEditsController(IStructuralEditService structuralEditService)
    {
        _structuralEditService = structuralEditService ?? throw new ArgumentNullException(nameof(structuralEditService));
    }

    [HttpPost]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(StructuralEditResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(StructuralEditResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StructuralEditResponse>> Apply(
        Guid projectId,
        [FromBody] ApplyStructuralEditRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Operation))
        {
            return BadRequest(new StructuralEditResponse
            {
                Success = false,
                Message = "operation is required.",
                AffectedPaths = []
            });
        }

        var createdBy = User.GetUserErn()
            ?? User.FindFirst("sub")?.Value
            ?? "system";

        var result = await _structuralEditService.ApplyAsync(
            projectId,
            new StructuralEditRequest(
                request.Operation,
                request.FragmentPath,
                request.OrderedFragmentPaths,
                request.Hidden),
            createdBy,
            cancellationToken);

        var response = new StructuralEditResponse
        {
            Success = result.Success,
            Message = result.Message,
            AffectedPaths = result.AffectedPaths
        };

        if (!result.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(Apply),
            new { projectId },
            response);
    }
}
