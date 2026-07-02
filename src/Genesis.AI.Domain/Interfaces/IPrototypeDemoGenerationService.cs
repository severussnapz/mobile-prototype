namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Generates a prototype-demo HTML page for a project.
/// </summary>
public interface IPrototypeDemoGenerationService
{
    /// <summary>
    /// Generates the prototype, inlines <c>emis-x-base.css</c>, and yields the
    /// fully-assembled HTML as a single chunk. Used by the synchronous command handler.
    /// </summary>
    IAsyncEnumerable<string> GenerateAsync(Guid projectId, string projectName, CancellationToken cancellationToken);

    /// <summary>
    /// Yields raw model output chunks with no CSS injection or buffering.
    /// The caller is responsible for passing the accumulated text through
    /// <see cref="PrototypeDocumentAssembler.Assemble"/> before surfacing it to clients.
    /// Used by the SSE streaming endpoint.
    /// </summary>
    IAsyncEnumerable<string> StreamRawAsync(Guid projectId, string projectName, CancellationToken cancellationToken);
}
