namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Assembles prototype/index.html from fragment files under prototype/fragments/.
/// Triggered automatically after any save_artefact or edit_artefact call on a fragment path.
/// Assembly is deterministic and zero LLM tokens.
/// </summary>
public interface IPrototypeAssemblyService
{
    /// <summary>
    /// Assembles all fragments into prototype/index.html.
    /// Fails closed: if assembly validation fails, the output is not persisted.
    /// </summary>
    Task AssemblePrototypeAsync(Guid projectId, CancellationToken cancellationToken);
}
