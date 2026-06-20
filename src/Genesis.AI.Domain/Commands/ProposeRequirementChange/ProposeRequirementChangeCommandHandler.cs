using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;

namespace Genesis.AI.Domain.Commands.ProposeRequirementChange;

public sealed class ProposeRequirementChangeCommandHandler
{
    private readonly IRequirementChangeRepository _repository;

    public ProposeRequirementChangeCommandHandler(IRequirementChangeRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ProposeRequirementChangeResult> Handle(
        ProposeRequirementChangeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var change = RequirementChange.Propose(
            projectId: command.ProjectId,
            reqId: command.ReqId,
            changeType: command.ChangeType,
            raisingPipeline: command.RaisingPipeline,
            raisingPipelineConversationId: command.RaisingPipelineConversationId,
            proposedAcText: command.ProposedAcText,
            rationale: command.Rationale,
            createdBy: command.CreatedBy);

        await _repository.AddAsync(change, cancellationToken);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new ProposeRequirementChangeResult(change.Id);
    }
}
