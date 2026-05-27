using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.CreateArtefacts;

public class CreateArtefactsCommandHandler : IRequestHandler<CreateArtefactsCommand, IReadOnlyList<Artefact>>
{
    private readonly IArtefactRepository _artefactRepository;
    private readonly TimeProvider _timeProvider;

    public CreateArtefactsCommandHandler(
        IArtefactRepository artefactRepository,
        TimeProvider timeProvider)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<IReadOnlyList<Artefact>> Handle(CreateArtefactsCommand request, CancellationToken cancellationToken)
    {
        var results = new List<Artefact>();

        foreach (var item in request.Artefacts)
        {
            if (string.IsNullOrWhiteSpace(item.FilePath) || string.IsNullOrWhiteSpace(item.Content))
                continue;

            var nextVersion = await _artefactRepository.GetNextVersionAsync(request.ProjectId, cancellationToken);

            var artefact = Artefact.CreateTextArtefact(
                request.ProjectId,
                nextVersion,
                item.FilePath.Trim(),
                item.ContentType ?? "text/markdown",
                item.Content,
                request.UserId,
                _timeProvider);

            await _artefactRepository.AddAsync(artefact, cancellationToken);
            results.Add(artefact);
        }

        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return results;
    }
}
