using System.Text.Json;
using AutoMapper;
using Genesis.AI.Api.Features.Notes;
using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;

namespace Genesis.AI.Tests.Api.Notes;

public sealed class NoteResourceAllFieldsMappingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IMapper _mapper;

    public NoteResourceAllFieldsMappingTests()
    {
        var mapperConfig = new MapperConfiguration(configuration =>
            configuration.AddProfile<NoteMappingProfile>());
        _mapper = mapperConfig.CreateMapper();
    }

    [Fact]
    public void NoteResource_MapsAndSerialisesAllFields()
    {
        // Arrange
        var timeProvider = TimeProvider.System;
        var note = new ProjectNote(
            Guid.NewGuid(),
            "Initial note content",
            "ern:note:1",
            "Ada",
            "Lovelace",
            timeProvider);

        // Act
        var resource = _mapper.Map<NoteResource>(note);
        var json = JsonSerializer.Serialize(resource, JsonOptions);

        // Assert
        var root = JsonDocument.Parse(json).RootElement;

        Assert.True(root.TryGetProperty("id", out var idElement), "id field missing");
        Assert.Equal(note.Id, idElement.GetGuid());

        Assert.True(root.TryGetProperty("projectId", out var projectIdElement), "projectId field missing");
        Assert.Equal(note.ProjectId, projectIdElement.GetGuid());

        Assert.True(root.TryGetProperty("content", out var contentElement), "content field missing");
        Assert.Equal(note.Content, contentElement.GetString());

        Assert.True(root.TryGetProperty("authorErn", out var authorErnElement), "authorErn field missing");
        Assert.Equal(note.AuthorErn, authorErnElement.GetString());

        Assert.True(root.TryGetProperty("authorGivenName", out var authorGivenNameElement), "authorGivenName field missing");
        Assert.Equal(note.AuthorGivenName, authorGivenNameElement.GetString());

        Assert.True(root.TryGetProperty("authorFamilyName", out var authorFamilyNameElement), "authorFamilyName field missing");
        Assert.Equal(note.AuthorFamilyName, authorFamilyNameElement.GetString());

        Assert.True(root.TryGetProperty("createdAt", out var createdAtElement), "createdAt field missing");
        Assert.Equal(note.CreatedAt, createdAtElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("updatedAt", out var updatedAtElement), "updatedAt field missing");
        Assert.Equal(note.UpdatedAt, updatedAtElement.GetDateTimeOffset());
    }
}
