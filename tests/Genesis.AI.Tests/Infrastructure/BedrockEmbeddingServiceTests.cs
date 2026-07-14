using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Genesis.AI.Tests.Infrastructure;

public class BedrockEmbeddingServiceTests
{
    private static InvokeModelResponse BuildResponse(float[] embedding)
    {
        var json = JsonSerializer.Serialize(new { embedding });
        return new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(json))
        };
    }

    [Fact]
    public async Task EmbedAsync_CallsInvokeModelAsync_WithCorrectModelIdAndBody()
    {
        var clientMock = new Mock<IAmazonBedrockRuntime>();
        clientMock
            .Setup(c => c.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResponse(new float[IEmbeddingService.Dimensions]));

        var sut = new BedrockEmbeddingService(clientMock.Object, NullLogger<BedrockEmbeddingService>.Instance);

        await sut.EmbedAsync("hello world", CancellationToken.None);

        clientMock.Verify(c => c.InvokeModelAsync(
            It.Is<InvokeModelRequest>(r =>
                r.ModelId == "amazon.titan-embed-text-v2:0"
                && r.ContentType == "application/json"
                && r.Accept == "application/json"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EmbedAsync_ParsesEmbeddingArrayFromResponse()
    {
        var expected = Enumerable.Range(0, IEmbeddingService.Dimensions)
            .Select(i => (float)i / IEmbeddingService.Dimensions)
            .ToArray();

        var clientMock = new Mock<IAmazonBedrockRuntime>();
        clientMock
            .Setup(c => c.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResponse(expected));

        var sut = new BedrockEmbeddingService(clientMock.Object, NullLogger<BedrockEmbeddingService>.Instance);

        var result = await sut.EmbedAsync("test text", CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task EmbedAsync_WhenResponseLengthWrong_ThrowsInvalidOperationException()
    {
        var clientMock = new Mock<IAmazonBedrockRuntime>();
        clientMock
            .Setup(c => c.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResponse(new float[512]));

        var sut = new BedrockEmbeddingService(clientMock.Object, NullLogger<BedrockEmbeddingService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.EmbedAsync("test", CancellationToken.None));
    }

    [Fact]
    public async Task EmbedAsync_WhenTextIsNullOrWhitespace_ThrowsArgumentException()
    {
        var clientMock = new Mock<IAmazonBedrockRuntime>();
        var sut = new BedrockEmbeddingService(clientMock.Object, NullLogger<BedrockEmbeddingService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.EmbedAsync("   ", CancellationToken.None));
    }
}
