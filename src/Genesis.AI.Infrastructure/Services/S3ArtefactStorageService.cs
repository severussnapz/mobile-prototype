using System.Linq;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Stores artefact content in Amazon S3 (or LocalStack for local development).
/// The bucket name is read from configuration and managed by infrastructure-as-code
/// in deployed environments (S3-003).
/// </summary>
public class S3ArtefactStorageService : IArtefactStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3ArtefactStorageService> _logger;
    private readonly string _bucketName;

    public S3ArtefactStorageService(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<S3ArtefactStorageService> logger)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bucketName = configuration["S3:ArtefactBucketName"]
            ?? throw new InvalidOperationException("Configuration 'S3:ArtefactBucketName' not found.");
    }

    public async Task<string> SaveContentAsync(
        Guid projectId,
        string filePath,
        int version,
        string content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var storageKey = BuildStorageKey(projectId, filePath, version);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            ContentBody = content,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        _logger.LogInformation(
            "Uploaded artefact content to S3: {StorageKey} ({Length} chars)",
            storageKey, content.Length);

        return storageKey;
    }

    public async Task<string?> GetContentAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _s3Client.GetObjectAsync(_bucketName, storageKey, cancellationToken);
            using var reader = new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Artefact content not found in S3: {StorageKey}", storageKey);
            return null;
        }
    }

    public async Task<string> SaveBinaryContentAsync(
        Guid projectId,
        string filePath,
        int version,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var storageKey = BuildStorageKey(projectId, filePath, version);

        using var contentStream = new MemoryStream(content, writable: false);
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            InputStream = contentStream,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        _logger.LogInformation(
            "Uploaded binary artefact content to S3: {StorageKey} ({Length} bytes)",
            storageKey, content.Length);

        return storageKey;
    }

    public async Task<byte[]?> GetBinaryContentAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _s3Client.GetObjectAsync(_bucketName, storageKey, cancellationToken);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            return memoryStream.ToArray();
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Binary artefact content not found in S3: {StorageKey}", storageKey);
            return null;
        }
    }

    public async Task DeleteContentAsync(string storageKey, CancellationToken cancellationToken)
    {
        await _s3Client.DeleteObjectAsync(_bucketName, storageKey, cancellationToken);

        _logger.LogInformation("Deleted artefact content from S3: {StorageKey}", storageKey);
    }

    public async Task<IReadOnlyList<(int Version, long SizeBytes, DateTimeOffset LastModified)>> ListVersionsAsync(
        Guid projectId,
        string filePath,
        CancellationToken cancellationToken)
    {
        var normalisedPath = filePath.TrimStart('/');
        var prefix = $"projects/{projectId}/artefacts/{normalisedPath}/";

        var versions = new List<(int Version, long SizeBytes, DateTimeOffset LastModified)>();

        try
        {
            string? continuationToken = null;
            do
            {
                var response = await _s3Client.ListObjectsV2Async(
                    new ListObjectsV2Request
                    {
                        BucketName = _bucketName,
                        Prefix = prefix,
                        ContinuationToken = continuationToken
                    },
                    cancellationToken);

                foreach (var s3Object in response.S3Objects)
                {
                    if (TryParseVersion(s3Object.Key, prefix, out var version))
                    {
                        versions.Add((
                            version,
                            s3Object.Size,
                            new DateTimeOffset(DateTime.SpecifyKind(s3Object.LastModified, DateTimeKind.Utc))));
                    }
                }

                continuationToken = response.IsTruncated ? response.NextContinuationToken : null;
            }
            while (continuationToken is not null);
        }
        catch (AmazonS3Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to list artefact versions from S3 for prefix {Prefix}; returning empty list.",
                prefix);
            return [];
        }

        return versions
            .OrderByDescending(entry => entry.Version)
            .ToList();
    }

    private static bool TryParseVersion(string key, string prefix, out int version)
    {
        version = 0;

        // Only direct children of the prefix are version objects (key = "{prefix}v{N}").
        var suffix = key[prefix.Length..];
        if (suffix.Length < 2 || suffix[0] != 'v' || suffix.Contains('/'))
        {
            return false;
        }

        return int.TryParse(suffix[1..], out version);
    }

    private static string BuildStorageKey(Guid projectId, string filePath, int version)
    {
        var normalisedPath = filePath.TrimStart('/');
        return $"projects/{projectId}/artefacts/{normalisedPath}/v{version}";
    }
}
