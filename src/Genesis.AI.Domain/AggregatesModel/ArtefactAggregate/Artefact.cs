using Genesis.AI.Core.Domain;

namespace Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;

public class Artefact : Entity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public int Version { get; private set; }
    public bool IsPublished { get; private set; }
    public string FilePath { get; private set; } = null!;
    public string S3Key { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long? SizeBytes { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Artefact() { } // Required for EF Core

    /// <summary>
    /// Creates an artefact whose content is stored in object storage (S3 / LocalStack).
    /// The database holds only metadata and the S3 key reference.
    /// </summary>
    public static Artefact CreateS3Artefact(
        Guid projectId,
        int version,
        string filePath,
        string s3Key,
        string contentType,
        long sizeBytes,
        string createdBy,
        TimeProvider timeProvider,
        bool isPublished)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(s3Key);

        var artefact = new Artefact
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Version = version,
            IsPublished = isPublished,
            FilePath = filePath,
            S3Key = s3Key,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            CreatedBy = createdBy,
            CreatedAt = timeProvider.GetUtcNow()
        };

        if (isPublished)
        {
            artefact.AddDomainEvent(new ArtefactPublishedDomainEvent(
                artefact.ProjectId,
                artefact.FilePath,
                artefact.S3Key,
                artefact.ContentType));
        }

        return artefact;
    }

    /// <summary>
    /// Replaces the stored content of this artefact in place — keeping the same identity
    /// (single row) but bumping the version number. Used for derived artefacts that should
    /// be regenerated rather than duplicated (e.g. the hazard log export), so re-running
    /// produces one artefact whose version climbs (v1 → v2 → … → vN).
    /// </summary>
    public void ReplaceContent(
        int version,
        string s3Key,
        string contentType,
        long sizeBytes,
        string updatedBy,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(s3Key);

        Version = version;
        S3Key = s3Key;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        CreatedBy = updatedBy;
        CreatedAt = timeProvider.GetUtcNow();
    }

    public bool PromoteToPublished()
    {
        if (IsPublished)
        {
            return false;
        }

        IsPublished = true;
        AddDomainEvent(new ArtefactPublishedDomainEvent(
            ProjectId,
            FilePath,
            S3Key,
            ContentType));
        return true;
    }
}
