using MediatR;

namespace Genesis.AI.Domain.Commands.SavePrototypeDemoHtml;

/// <summary>
/// Persists a generated prototype-demo HTML document as a versioned project artefact
/// under <c>prototype-demo/index.html</c> so it survives a page refresh. Re-saving
/// bumps the version of the single artefact row in place (v1 → v2 → … → vN).
/// </summary>
public sealed record SavePrototypeDemoHtmlCommand(
    Guid ProjectId,
    string Html,
    string UserId) : IRequest<SavePrototypeDemoHtmlResult>;
