namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Abstraction over artefact content storage (S3 in deployed environments,
/// LocalStack locally). Metadata remains in the database; the raw content lives
/// in object storage and is referenced by the returned key.
/// </summary>
public interface IArtefactStorageService
{
    /// <summary>
    /// Uploads artefact content to object storage and returns the storage key
    /// to persist against the artefact record.
    /// </summary>
    Task<string> SaveContentAsync(
        Guid projectId,
        string filePath,
        int version,
        string content,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves artefact content from object storage by its storage key.
    /// Returns <c>null</c> if the object does not exist.
    /// </summary>
    Task<string?> GetContentAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>
    /// Uploads binary artefact content (e.g. a spreadsheet) to object storage and
    /// returns the storage key to persist against the artefact record.
    /// </summary>
    Task<string> SaveBinaryContentAsync(
        Guid projectId,
        string filePath,
        int version,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves binary artefact content from object storage by its storage key.
    /// Returns <c>null</c> if the object does not exist.
    /// </summary>
    Task<byte[]?> GetBinaryContentAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes artefact content from object storage by its storage key.
    /// No-op if the object does not exist.
    /// </summary>
    Task DeleteContentAsync(string storageKey, CancellationToken cancellationToken);
}
