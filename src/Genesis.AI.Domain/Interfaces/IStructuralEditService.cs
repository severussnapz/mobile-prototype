namespace Genesis.AI.Domain.Interfaces;

public interface IStructuralEditService
{
    Task<StructuralEditResult> ApplyAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken);
}
