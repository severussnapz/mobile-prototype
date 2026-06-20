using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public interface IStructuralEditReorderService
{
    Task<StructuralEditResult> ApplyAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken);
}
