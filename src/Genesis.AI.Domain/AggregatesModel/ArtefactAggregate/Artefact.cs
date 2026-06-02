using Genesis.AI.Core.Domain;

namespace Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;

public class Artefact : Entity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public int Version { get; private set; }
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
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(s3Key);

        return new Artefact
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Version = version,
            FilePath = filePath,
            S3Key = s3Key,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            CreatedBy = createdBy,
            CreatedAt = timeProvider.GetUtcNow()
        };
    }
}
