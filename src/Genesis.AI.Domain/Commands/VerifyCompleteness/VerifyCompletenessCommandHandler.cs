using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.VerifyCompleteness;

public sealed class VerifyCompletenessCommandHandler
    : IRequestHandler<VerifyCompletenessCommand, VerifyCompletenessResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly INormalisationGateService _normalisationGateService;

    public VerifyCompletenessCommandHandler(
        IProjectRepository projectRepository,
        INormalisationGateService normalisationGateService)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _normalisationGateService = normalisationGateService ?? throw new ArgumentNullException(nameof(normalisationGateService));
    }

    public async Task<VerifyCompletenessResult> Handle(
        VerifyCompletenessCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return VerifyCompletenessResult.Failure(
                VerifyCompletenessStatus.ProjectNotFound,
                $"No project found with ID '{request.ProjectId}'.");
        }

        var evaluation = await _normalisationGateService.EvaluateAsync(
            request.ProjectId,
            project.Code,
            cancellationToken);

        return new VerifyCompletenessResult(
            VerifyCompletenessStatus.Success,
            evaluation.GatePassed,
            evaluation.Errors,
            evaluation.OutputArtefacts,
            null);
    }
}
