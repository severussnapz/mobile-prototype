using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.CreateDecision;

public class CreateDecisionCommandHandler : IRequestHandler<CreateDecisionCommand, CreateDecisionResult>
{
    private readonly IProjectDecisionRepository _decisionRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly TimeProvider _timeProvider;

    public CreateDecisionCommandHandler(
        IProjectDecisionRepository decisionRepository,
        IProjectRepository projectRepository,
        TimeProvider timeProvider)
    {
        _decisionRepository = decisionRepository ?? throw new ArgumentNullException(nameof(decisionRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CreateDecisionResult> Handle(CreateDecisionCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
            return new CreateDecisionResult(ProjectFound: false);

        var decision = new ProjectDecision(
            request.ProjectId,
            request.Title,
            request.Context,
            request.Decision,
            request.Consequences,
            request.AuthorErn,
            request.AuthorGivenName,
            request.AuthorFamilyName,
            _timeProvider);

        await _decisionRepository.AddAsync(decision, cancellationToken);
        await _decisionRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateDecisionResult(ProjectFound: true, Decision: decision);
    }
}
