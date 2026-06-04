using Genesis.AI.Core.Domain;

namespace Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;

/// <summary>
/// Aggregate root representing an ADR-style decision recorded against a project.
/// Decisions are standalone and are never included in AI conversation context.
/// </summary>
public class ProjectDecision : Entity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Context { get; private set; } = null!;
    public string Decision { get; private set; } = null!;
    public string Consequences { get; private set; } = null!;
    public string? AuthorErn { get; private set; }
    public string? AuthorGivenName { get; private set; }
    public string? AuthorFamilyName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ProjectDecision() { } // Required for EF Core

    public ProjectDecision(
        Guid projectId,
        string title,
        string context,
        string decision,
        string consequences,
        string? authorErn,
        string? authorGivenName,
        string? authorFamilyName,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(decision);
        ArgumentException.ThrowIfNullOrWhiteSpace(consequences);

        Id = Guid.NewGuid();
        ProjectId = projectId;
        Title = title;
        Context = context;
        Decision = decision;
        Consequences = consequences;
        AuthorErn = authorErn;
        AuthorGivenName = authorGivenName;
        AuthorFamilyName = authorFamilyName;
        CreatedAt = timeProvider.GetUtcNow();
        UpdatedAt = CreatedAt;
    }

    public void Update(
        string title,
        string context,
        string decision,
        string consequences,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(decision);
        ArgumentException.ThrowIfNullOrWhiteSpace(consequences);

        Title = title;
        Context = context;
        Decision = decision;
        Consequences = consequences;
        UpdatedAt = timeProvider.GetUtcNow();
    }
}
