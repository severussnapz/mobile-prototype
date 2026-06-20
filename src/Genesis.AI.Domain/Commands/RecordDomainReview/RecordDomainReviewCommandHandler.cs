using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;

namespace Genesis.AI.Domain.Commands.RecordDomainReview;

public sealed class RecordDomainReviewCommandHandler
{
    private readonly IRequirementChangeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RecordDomainReviewCommandHandler(
        IRequirementChangeRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task Handle(
        RecordDomainReviewCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var change = await _repository.GetByIdAsync(command.ChangeId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Requirement change '{command.ChangeId}' not found.");

        switch (command.Domain)
        {
            case ReviewDomain.ClinicalSafety:
                change.RecordClinicalSafetyReview(command.Reviewer, _timeProvider);
                break;
            case ReviewDomain.InformationGovernance:
                change.RecordIgReview(command.Reviewer, _timeProvider);
                break;
            case ReviewDomain.Security:
                change.RecordSecurityReview(command.Reviewer, _timeProvider);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown review domain: {command.Domain}");
        }

        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
