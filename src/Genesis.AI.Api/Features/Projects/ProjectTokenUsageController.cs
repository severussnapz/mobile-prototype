using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Projects;

[ApiController]
[Route("api/v1/projects")]
[Produces("application/json")]
public sealed class ProjectTokenUsageController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectTokenUsageController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Get aggregated token usage and estimated cost for all stages in a project.
    /// </summary>
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [HttpGet("{id:guid}/token-usage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjectTokenUsage(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new Domain.Queries.GetProjectTokenUsage.GetProjectTokenUsageQuery(id), cancellationToken);

        return Ok(new ApiResponse<ProjectTokenUsageResponse>
        {
            Data = new ProjectTokenUsageResponse
            {
                Stages = result.Stages,
                Totals = new TokenUsageTotals
                {
                    InputTokens = result.TotalInputTokens,
                    OutputTokens = result.TotalOutputTokens,
                    CacheReadInputTokens = result.TotalCacheReadInputTokens,
                    CacheWriteInputTokens = result.TotalCacheWriteInputTokens,
                    TurnCount = result.TotalTurnCount,
                    EstimatedCost = result.TotalEstimatedCost
                }
            }
        });
    }
}
