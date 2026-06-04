using AutoMapper;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Commands.CreateDecision;
using Genesis.AI.Domain.Commands.DeleteDecision;
using Genesis.AI.Domain.Commands.UpdateDecision;
using Genesis.AI.Domain.Queries.GetProjectDecisions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.Decisions;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/decisions")]
[Produces("application/json")]
[Consumes("application/json")]
public class DecisionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public DecisionsController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Lists all decisions recorded against a project.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorisationPolicies.ProjectRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DecisionResource>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDecisions(Guid projectId, CancellationToken cancellationToken)
    {
        var decisions = await _mediator.Send(new GetProjectDecisionsQuery(projectId), cancellationToken);

        if (decisions is null)
            return ProjectNotFound(projectId);

        var resources = _mapper.Map<IReadOnlyList<DecisionResource>>(decisions);
        return Ok(new ApiResponse<IReadOnlyList<DecisionResource>> { Data = resources });
    }

    /// <summary>
    /// Creates a new decision against a project.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ApiResponse<DecisionResource>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDecision(
        Guid projectId,
        [FromBody] CreateDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDecisionCommand(
            projectId,
            request.Title,
            request.Context,
            request.Decision,
            request.Consequences,
            User.GetUserErn(),
            User.GetGivenName(),
            User.GetFamilyName());

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.ProjectFound)
            return ProjectNotFound(projectId);

        var resource = _mapper.Map<DecisionResource>(result.Decision);
        return CreatedAtAction(
            nameof(GetDecisions),
            new { projectId },
            new ApiResponse<DecisionResource> { Data = resource });
    }

    /// <summary>
    /// Updates an existing decision.
    /// </summary>
    [HttpPut("{decisionId:guid}")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(typeof(ApiResponse<DecisionResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDecision(
        Guid projectId,
        Guid decisionId,
        [FromBody] UpdateDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateDecisionCommand(
                projectId,
                decisionId,
                request.Title,
                request.Context,
                request.Decision,
                request.Consequences),
            cancellationToken);

        if (!result.Found)
            return DecisionNotFound(decisionId);

        var resource = _mapper.Map<DecisionResource>(result.Decision);
        return Ok(new ApiResponse<DecisionResource> { Data = resource });
    }

    /// <summary>
    /// Deletes a decision.
    /// </summary>
    [HttpDelete("{decisionId:guid}")]
    [Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDecision(
        Guid projectId,
        Guid decisionId,
        CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(new DeleteDecisionCommand(projectId, decisionId), cancellationToken);

        if (!deleted)
            return DecisionNotFound(decisionId);

        return NoContent();
    }

    private NotFoundObjectResult ProjectNotFound(Guid projectId)
    {
        return NotFound(ApiErrorResponse.Create(
            "404",
            "Project not found",
            $"No project found with ID '{projectId}'."));
    }

    private NotFoundObjectResult DecisionNotFound(Guid decisionId)
    {
        return NotFound(ApiErrorResponse.Create(
            "404",
            "Decision not found",
            $"No decision found with ID '{decisionId}'."));
    }
}
