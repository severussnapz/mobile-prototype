using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

public interface ISessionCloseContextBuilder
{
    Task<string> BuildSessionCloseContextAsync(Guid projectId, StageType stageType, CancellationToken cancellationToken);
}