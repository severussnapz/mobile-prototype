using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Genesis.AI.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Tests.Services;

public class S3ArtefactStorageServiceTests
{
    private const string BucketName = "genesis-ai-artefacts";

    private readonly Mock<IAmazonS3> _s3ClientMock = new();
    private readonly Mock<ILogger<S3ArtefactStorageService>> _loggerMock = new();
    private readonly IConfiguration _configuration;

    public S3ArtefactStorageServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["S3:ArtefactBucketName"] = BucketName
            })
            .Build();
    }

    private S3ArtefactStorageService CreateService() =>
        new(_s3ClientMock.Object, _configuration, _loggerMock.Object);

    [Fact]
    public void Constructor_WithNullS3Client_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new S3ArtefactStorageService(null!, _configuration, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithMissingBucketConfiguration_ThrowsInvalidOperationException()
    {
        var emptyConfiguration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() =>
            new S3ArtefactStorageService(_s3ClientMock.Object, emptyConfiguration, _loggerMock.Object));
    }

    [Fact]
    public async Task SaveContentAsync_WhenCalled_ReturnsKeyMatchingScheme()
    {
        var projectId = Guid.Parse("03735ad1-8759-414e-a93f-ce8cc7bfc1fc");
        _s3ClientMock
            .Setup(client => client.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());
        var service = CreateService();

        var storageKey = await service.SaveContentAsync(
            projectId, "requirements/REQ-001.md", 2, "# Content", "text/markdown", CancellationToken.None);

        Assert.Equal(
            $"projects/{projectId}/artefacts/requirements/REQ-001.md/v2",
            storageKey);
    }

    [Fact]
    public async Task SaveContentAsync_WithLeadingSlashInFilePath_NormalisesKey()
    {
        var projectId = Guid.NewGuid();
        _s3ClientMock
            .Setup(client => client.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());
        var service = CreateService();

        var storageKey = await service.SaveContentAsync(
            projectId, "/manifest.md", 1, "content", "text/markdown", CancellationToken.None);

        Assert.Equal($"projects/{projectId}/artefacts/manifest.md/v1", storageKey);
    }

    [Fact]
    public async Task SaveContentAsync_WhenCalled_SendsExpectedPutObjectRequest()
    {
        var projectId = Guid.NewGuid();
        PutObjectRequest? capturedRequest = null;
        _s3ClientMock
            .Setup(client => client.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new PutObjectResponse());
        var service = CreateService();

        await service.SaveContentAsync(
            projectId, "manifest.md", 3, "# Hello", "text/markdown", CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal(BucketName, capturedRequest!.BucketName);
        Assert.Equal($"projects/{projectId}/artefacts/manifest.md/v3", capturedRequest.Key);
        Assert.Equal("# Hello", capturedRequest.ContentBody);
        Assert.Equal("text/markdown", capturedRequest.ContentType);
    }

    [Fact]
    public async Task GetContentAsync_WhenObjectExists_ReturnsContent()
    {
        const string expected = "# Stored artefact content";
        var response = new GetObjectResponse
        {
            ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(expected))
        };
        _s3ClientMock
            .Setup(client => client.GetObjectAsync(
                BucketName, "some-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var service = CreateService();

        var content = await service.GetContentAsync("some-key", CancellationToken.None);

        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task GetContentAsync_WhenObjectNotFound_ReturnsNull()
    {
        _s3ClientMock
            .Setup(client => client.GetObjectAsync(
                BucketName, "missing-key", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
        var service = CreateService();

        var content = await service.GetContentAsync("missing-key", CancellationToken.None);

        Assert.Null(content);
    }

    [Fact]
    public async Task GetContentAsync_WhenS3ErrorIsNotNotFound_Rethrows()
    {
        _s3ClientMock
            .Setup(client => client.GetObjectAsync(
                BucketName, "denied-key", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Access denied") { StatusCode = HttpStatusCode.Forbidden });
        var service = CreateService();

        await Assert.ThrowsAsync<AmazonS3Exception>(() =>
            service.GetContentAsync("denied-key", CancellationToken.None));
    }

    [Fact]
    public async Task DeleteContentAsync_WhenCalled_DeletesObjectByBucketAndKey()
    {
        _s3ClientMock
            .Setup(client => client.DeleteObjectAsync(
                BucketName, "key-to-delete", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse());
        var service = CreateService();

        await service.DeleteContentAsync("key-to-delete", CancellationToken.None);

        _s3ClientMock.Verify(
            client => client.DeleteObjectAsync(BucketName, "key-to-delete", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
