using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;

namespace Genesis.AI.Tests.Domain;

public class ProjectNoteTests
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public void Constructor_WithValidContent_SetsProperties()
    {
        var projectId = Guid.NewGuid();

        var note = new ProjectNote(projectId, "Remember this", "ern-1", "Ada", "Lovelace", _timeProvider);

        Assert.Equal(projectId, note.ProjectId);
        Assert.Equal("Remember this", note.Content);
        Assert.Equal("ern-1", note.AuthorErn);
        Assert.Equal("Ada", note.AuthorGivenName);
        Assert.Equal("Lovelace", note.AuthorFamilyName);
        Assert.Equal(note.CreatedAt, note.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithBlankContent_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectNote(Guid.NewGuid(), "  ", null, null, null, _timeProvider));
    }

    [Fact]
    public void UpdateContent_WithNewContent_UpdatesContent()
    {
        var note = new ProjectNote(Guid.NewGuid(), "Old", null, null, null, _timeProvider);

        note.UpdateContent("New", _timeProvider);

        Assert.Equal("New", note.Content);
    }
}
