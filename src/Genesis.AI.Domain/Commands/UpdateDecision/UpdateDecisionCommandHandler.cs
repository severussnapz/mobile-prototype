using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.UpdateDecision;

public class UpdateDecisionCommandHandler : IRequestHandler<UpdateDecisionCommand, UpdateDecisionResult>
{
    private readonly IProjectDecisionRepository _decisionRepository;
    private readonly TimeProvider _timeProvider;

    public UpdateDecisionCommandHandler(
        IProjectDecisionRepository decisionRepository,
        TimeProvider timeProvider)
    {
        _decisionRepository = decisionRepository ?? throw new ArgumentNullException(nameof(decisionRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<UpdateDecisionResult> Handle(UpdateDecisionCommand request, CancellationToken cancellationToken)
    {
        var decision = await _decisionRepository.GetByIdAsync(request.DecisionId, cancellationToken);
        if (decision is null || decision.ProjectId != request.ProjectId)
            return new UpdateDecisionResult(Found: false);

        decision.Update(
            request.Title,
            request.Context,
            request.Decision,
            request.Consequences,
            _timeProvider);

        await _decisionRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateDecisionResult(Found: true, Decision: decision);
    }
}
