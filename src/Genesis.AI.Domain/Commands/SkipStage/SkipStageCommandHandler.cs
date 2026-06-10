using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.SkipStage;

public class SkipStageCommandHandler : IRequestHandler<SkipStageCommand, SkipStageResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly TimeProvider _timeProvider;

    public SkipStageCommandHandler(IProjectRepository projectRepository, TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SkipStageResult> Handle(SkipStageCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByStageIdAsync(request.StageId, cancellationToken);
        if (project is null)
            return new SkipStageResult(Found: false, ValidationError: null);

        var stage = project.PipelineStages.First(pipelineStage => pipelineStage.Id == request.StageId);

        if (stage.Status == PipelineStageStatus.Complete)
            return new SkipStageResult(Found: true, ValidationError: "Cannot skip a completed stage.");

        stage.Skip();
        project.RecalculateStatus(_timeProvider);
        await _projectRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new SkipStageResult(
            Found: true,
            ValidationError: null,
            StageId: stage.Id,
            StageType: ConvertToSnakeCase(stage.StageType.ToString()),
            Status: "complete");
    }

    private static string ConvertToSnakeCase(string value)
    {
        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $"_{character}" : character.ToString()))
            .ToLowerInvariant();
    }
}
