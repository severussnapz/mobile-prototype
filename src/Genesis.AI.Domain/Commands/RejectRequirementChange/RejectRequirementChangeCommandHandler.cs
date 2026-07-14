using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;

namespace Genesis.AI.Domain.Commands.RejectRequirementChange;

public sealed class RejectRequirementChangeCommandHandler
{
    private readonly IRequirementChangeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RejectRequirementChangeCommandHandler(
        IRequirementChangeRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task Handle(
        RejectRequirementChangeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var change = await _repository.GetByIdForProjectAsync(
            command.ChangeId,
            command.ProjectId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Requirement change '{command.ChangeId}' not found.");

        change.Reject(rejectedBy: command.RejectedBy, timeProvider: _timeProvider);

        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
