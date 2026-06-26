using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IChangeFileWriterService
{
    Task WriteChangeFileAsync(
        RequirementChange change,
        CancellationToken cancellationToken);
}
