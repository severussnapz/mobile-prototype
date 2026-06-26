using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public interface IStructuralEditMutationService
{
    Task<StructuralEditResult> ToggleVisibilityAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken);

    Task<StructuralEditResult> DuplicateAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken);

    Task<StructuralEditResult> DeleteAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken);
}
