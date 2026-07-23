using System.Text.Json;
using AutoMapper;
using Genesis.AI.Api.Features.Decisions;
using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;

namespace Genesis.AI.Tests.Api.Decisions;

public sealed class DecisionResourceAllFieldsMappingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IMapper _mapper;

    public DecisionResourceAllFieldsMappingTests()
    {
        var mapperConfig = new MapperConfiguration(configuration =>
            configuration.AddProfile<DecisionMappingProfile>());
        _mapper = mapperConfig.CreateMapper();
    }

    [Fact]
    public void DecisionResource_MapsAndSerialisesAllFields()
    {
        // Arrange
        var timeProvider = TimeProvider.System;
        var decision = new ProjectDecision(
            Guid.NewGuid(),
            "Use MediatR",
            "Need command/query separation",
            "Adopt MediatR for CQRS",
            "Slightly more boilerplate",
            "ern:decision:1",
            "Grace",
            "Hopper",
            timeProvider);

        // Act
        var resource = _mapper.Map<DecisionResource>(decision);
        var json = JsonSerializer.Serialize(resource, JsonOptions);

        // Assert
        var root = JsonDocument.Parse(json).RootElement;

        Assert.True(root.TryGetProperty("id", out var idElement), "id field missing");
        Assert.Equal(decision.Id, idElement.GetGuid());

        Assert.True(root.TryGetProperty("projectId", out var projectIdElement), "projectId field missing");
        Assert.Equal(decision.ProjectId, projectIdElement.GetGuid());

        Assert.True(root.TryGetProperty("title", out var titleElement), "title field missing");
        Assert.Equal(decision.Title, titleElement.GetString());

        Assert.True(root.TryGetProperty("context", out var contextElement), "context field missing");
        Assert.Equal(decision.Context, contextElement.GetString());

        Assert.True(root.TryGetProperty("decision", out var decisionElement), "decision field missing");
        Assert.Equal(decision.Decision, decisionElement.GetString());

        Assert.True(root.TryGetProperty("consequences", out var consequencesElement), "consequences field missing");
        Assert.Equal(decision.Consequences, consequencesElement.GetString());

        Assert.True(root.TryGetProperty("authorErn", out var authorErnElement), "authorErn field missing");
        Assert.Equal(decision.AuthorErn, authorErnElement.GetString());

        Assert.True(root.TryGetProperty("authorGivenName", out var authorGivenNameElement), "authorGivenName field missing");
        Assert.Equal(decision.AuthorGivenName, authorGivenNameElement.GetString());

        Assert.True(root.TryGetProperty("authorFamilyName", out var authorFamilyNameElement), "authorFamilyName field missing");
        Assert.Equal(decision.AuthorFamilyName, authorFamilyNameElement.GetString());

        Assert.True(root.TryGetProperty("createdAt", out var createdAtElement), "createdAt field missing");
        Assert.Equal(decision.CreatedAt, createdAtElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("updatedAt", out var updatedAtElement), "updatedAt field missing");
        Assert.Equal(decision.UpdatedAt, updatedAtElement.GetDateTimeOffset());
    }
}
