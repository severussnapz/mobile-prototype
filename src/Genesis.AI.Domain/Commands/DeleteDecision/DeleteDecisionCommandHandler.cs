using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.DeleteDecision;

public class DeleteDecisionCommandHandler : IRequestHandler<DeleteDecisionCommand, bool>
{
    private readonly IProjectDecisionRepository _decisionRepository;

    public DeleteDecisionCommandHandler(IProjectDecisionRepository decisionRepository)
    {
        _decisionRepository = decisionRepository ?? throw new ArgumentNullException(nameof(decisionRepository));
    }

    public async Task<bool> Handle(DeleteDecisionCommand request, CancellationToken cancellationToken)
    {
        var decision = await _decisionRepository.GetByIdAsync(request.DecisionId, cancellationToken);
        if (decision is null || decision.ProjectId != request.ProjectId)
            return false;

        _decisionRepository.Remove(decision);
        await _decisionRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
