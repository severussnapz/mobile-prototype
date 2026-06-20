using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class StructuralEditService : IStructuralEditService
{
    private readonly IStructuralEditReorderService _reorderService;
    private readonly IStructuralEditMutationService _mutationService;

    public StructuralEditService(
        IStructuralEditReorderService reorderService,
        IStructuralEditMutationService mutationService)
    {
        _reorderService = reorderService ?? throw new ArgumentNullException(nameof(reorderService));
        _mutationService = mutationService ?? throw new ArgumentNullException(nameof(mutationService));
    }

    public async Task<StructuralEditResult> ApplyAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken)
    {
        var operation = request.Operation.Trim().ToLowerInvariant();
        return operation switch
        {
            "reorder" => await _reorderService.ApplyAsync(projectId, request, createdBy, cancellationToken),
            "toggle_visibility" => await _mutationService.ToggleVisibilityAsync(projectId, request, createdBy, cancellationToken),
            "duplicate" => await _mutationService.DuplicateAsync(projectId, request, createdBy, cancellationToken),
            "delete" => await _mutationService.DeleteAsync(projectId, request, createdBy, cancellationToken),
            _ => new StructuralEditResult(false, "Unsupported structural edit operation.", [])
        };
    }
}
