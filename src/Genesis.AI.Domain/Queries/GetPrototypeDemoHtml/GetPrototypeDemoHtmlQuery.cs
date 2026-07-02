using MediatR;

namespace Genesis.AI.Domain.Queries.GetPrototypeDemoHtml;

/// <summary>
/// Loads the saved prototype-demo HTML document (<c>prototype-demo/index.html</c>)
/// for a project so the page can restore it on load without regenerating.
/// </summary>
public sealed record GetPrototypeDemoHtmlQuery(Guid ProjectId) : IRequest<GetPrototypeDemoHtmlResult>;
