using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

public sealed class BedrockEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly IAmazonBedrockRuntime _client;
    private readonly string _embeddingModelId;
    private readonly bool _ownsClient;
    private readonly ILogger<BedrockEmbeddingService> _logger;

    public BedrockEmbeddingService(IConfiguration configuration, ILogger<BedrockEmbeddingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var region = configuration["Bedrock:Region"] ?? "eu-west-2";
        _embeddingModelId = configuration["Bedrock:EmbeddingModelId"] ?? "amazon.titan-embed-text-v2:0";

        _client = new AmazonBedrockRuntimeClient(RegionEndpoint.GetBySystemName(region));
        _ownsClient = true;

        _logger.LogInformation(
            "BedrockEmbeddingService configured: model={ModelId}, region={Region}",
            _embeddingModelId, region);
    }

    // Internal constructor for unit testing — accepts a pre-built IAmazonBedrockRuntime mock.
    internal BedrockEmbeddingService(IAmazonBedrockRuntime client, ILogger<BedrockEmbeddingService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _embeddingModelId = "amazon.titan-embed-text-v2:0";
        _ownsClient = false;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var bodyJson = JsonSerializer.Serialize(new
        {
            inputText = text,
            dimensions = IEmbeddingService.Dimensions,
            normalize = true
        });

        var request = new InvokeModelRequest
        {
            ModelId = _embeddingModelId,
            ContentType = "application/json",
            Accept = "application/json",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyJson))
        };

        var response = await _client.InvokeModelAsync(request, cancellationToken);

        var responseBody = response.Body;
        if (responseBody.CanSeek)
        {
            responseBody.Position = 0;
        }

        using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: false);
        var responseJson = await reader.ReadToEndAsync(cancellationToken);

        using var doc = JsonDocument.Parse(responseJson);
        var embedding = doc.RootElement
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(element => element.GetSingle())
            .ToArray();

        if (embedding.Length != IEmbeddingService.Dimensions)
        {
            throw new InvalidOperationException(
                $"Bedrock returned embedding with {embedding.Length} dimensions; expected {IEmbeddingService.Dimensions}.");
        }

        return embedding;
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }
}
