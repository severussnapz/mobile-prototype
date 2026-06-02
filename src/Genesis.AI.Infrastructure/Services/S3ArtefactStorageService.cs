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

    public async Task DeleteContentAsync(string storageKey, CancellationToken cancellationToken)
    {
        await _s3Client.DeleteObjectAsync(_bucketName, storageKey, cancellationToken);

        _logger.LogInformation("Deleted artefact content from S3: {StorageKey}", storageKey);
    }

    private static string BuildStorageKey(Guid projectId, string filePath, int version)
    {
        var normalisedPath = filePath.TrimStart('/');
        return $"projects/{projectId}/artefacts/{normalisedPath}/v{version}";
    }
}
