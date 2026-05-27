using Genesis.AI.Core.Domain;

namespace Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;

public class Artefact : Entity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public int Version { get; private set; }
    public string FilePath { get; private set; } = null!;
    public string? S3Key { get; private set; }
    public string ContentType { get; private set; } = null!;
    public string? Content { get; private set; }
    public long? SizeBytes { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Artefact() { } // Required for EF Core

    /// <summary>
    /// Creates a text-based artefact stored in the database (markdown, JSON).
    /// </summary>
    public static Artefact CreateTextArtefact(
        Guid projectId,
        int version,
        string filePath,
        string contentType,
        string content,
        string createdBy,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new Artefact
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Version = version,
            FilePath = filePath,
            ContentType = contentType,
            Content = content,
            SizeBytes = System.Text.Encoding.UTF8.GetByteCount(content),
            CreatedBy = createdBy,
            CreatedAt = timeProvider.GetUtcNow()
        };
    }

    /// <summary>
    /// Creates an S3-backed artefact (user-uploaded files: PDFs, images, docs).
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
