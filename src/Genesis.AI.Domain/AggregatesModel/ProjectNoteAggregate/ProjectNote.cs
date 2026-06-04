using Genesis.AI.Core.Domain;

namespace Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;

/// <summary>
/// Aggregate root representing a free-text note recorded against a project.
/// Notes are standalone and are never included in AI conversation context.
/// </summary>
public class ProjectNote : Entity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Content { get; private set; } = null!;
    public string? AuthorErn { get; private set; }
    public string? AuthorGivenName { get; private set; }
    public string? AuthorFamilyName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ProjectNote() { } // Required for EF Core

    public ProjectNote(
        Guid projectId,
        string content,
        string? authorErn,
        string? authorGivenName,
        string? authorFamilyName,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Id = Guid.NewGuid();
        ProjectId = projectId;
        Content = content;
        AuthorErn = authorErn;
        AuthorGivenName = authorGivenName;
        AuthorFamilyName = authorFamilyName;
        CreatedAt = timeProvider.GetUtcNow();
        UpdatedAt = CreatedAt;
    }

    public void UpdateContent(string content, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Content = content;
        UpdatedAt = timeProvider.GetUtcNow();
    }
}
