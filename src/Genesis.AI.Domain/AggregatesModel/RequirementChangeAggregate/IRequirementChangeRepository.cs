using Genesis.AI.Core.Data;

namespace Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;

public interface IRequirementChangeRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task AddAsync(RequirementChange change, CancellationToken cancellationToken);

    Task<RequirementChange?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<RequirementChange>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RequirementChange>> GetPendingByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    Task<bool> HasOpenDefiniteReviewsAsync(
        Guid projectId,
        CancellationToken cancellationToken);
}
