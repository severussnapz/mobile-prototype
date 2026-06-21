using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Api.Features.PipelineReadiness;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/pipeline-readiness")]
[Authorize(Policy = AuthorisationPolicies.ConversationWrite)]
[Produces("application/json")]
public sealed class PipelineReadinessController : ControllerBase
{
    private readonly IPipelineReadinessService _readinessService;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;

    public PipelineReadinessController(
        IPipelineReadinessService readinessService,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService)
    {
        _readinessService = readinessService ??
            throw new ArgumentNullException(nameof(readinessService));
        _artefactRepository = artefactRepository ??
            throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ??
            throw new ArgumentNullException(nameof(artefactStorageService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PipelineReadinessResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReadiness(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var artefacts = await _artefactRepository.GetByProjectIdAsync(
            projectId, cancellationToken);

        var reqArtefacts = artefacts
            .Where(artefact => artefact.FilePath.StartsWith("requirements/REQ-",
                StringComparison.OrdinalIgnoreCase) &&
                artefact.FilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var reqContents = new Dictionary<string, string>();
        foreach (var artefact in reqArtefacts)
        {
            var content = await _artefactStorageService.GetContentAsync(
                artefact.S3Key, cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var reqId = Path.GetFileNameWithoutExtension(artefact.FilePath);
            reqContents[reqId] = content;
        }

        var result = await _readinessService.GetReadinessAsync(
            projectId, reqContents, cancellationToken);

        return Ok(new ApiResponse<PipelineReadinessResponse>
        {
            Data = new PipelineReadinessResponse(result.IsReady, result.Blockers)
        });
    }
}
