namespace Genesis.AI.Domain.Interfaces;

public interface IPrototypeDemoEditService
{
    Task<PrototypeElementEditResult> EditElementAsync(
        Guid projectId,
        PrototypeElementEditRequest request,
        CancellationToken cancellationToken);
}
