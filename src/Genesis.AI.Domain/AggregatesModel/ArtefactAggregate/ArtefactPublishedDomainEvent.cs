using MediatR;

namespace Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;

/// <summary>
/// Raised when an artefact becomes published — either born-published on creation or
/// promoted from draft. Handled post-dispatch to index the artefact content into the
/// project knowledge namespace (pgvector) and push to GitHub. Fires from within the aggregate
/// so the side effect is bound to the artefact's own unit of work, immune to the DbContext/scope
/// mismatches that plagued the previous ChangeTracker-sniffing interceptor.
/// </summary>
public sealed record ArtefactPublishedDomainEvent : INotification
{
    public Guid ProjectId { get; init; }
    public string FilePath { get; init; }
    public string S3Key { get; init; }
    public string ContentType { get; init; }
    public Guid ArtefactId { get; init; }
    public int Version { get; init; }
    public string TriggeredBy { get; init; }
    public string PublishedByErn => TriggeredBy;

    public ArtefactPublishedDomainEvent(
        Guid projectId,
        string filePath,
        string s3Key,
        string contentType,
        Guid artefactId,
        int version,
        string triggeredBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(triggeredBy);
        ProjectId = projectId;
        FilePath = filePath;
        S3Key = s3Key;
        ContentType = contentType;
        ArtefactId = artefactId;
        Version = version;
        TriggeredBy = triggeredBy;
    }
}
