using Genesis.AI.Api.Features.Artefacts;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class ArtefactControllerPrefixTests
{
    [Fact]
    public void PrefixFilter_WhenChangesPrefix_ReturnsOnlyChangeFiles()
    {
        var summaries = new List<ArtefactSummaryResponse>
        {
            new() { FilePath = "requirements/REQ-001.md" },
            new() { FilePath = "requirements/REQ-002.md" },
            new() { FilePath = "changes/CHANGE-001.md" },
            new() { FilePath = "changes/CHANGE-002.md" },
            new() { FilePath = "prototype/index.html" }
        };

        var filtered = ApplyPrefixFilter(summaries, "changes/");

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, item =>
            Assert.StartsWith("changes/", item.FilePath, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PrefixFilter_WhenRequirementsPrefix_ReturnsOnlyReqFiles()
    {
        var summaries = new List<ArtefactSummaryResponse>
        {
            new() { FilePath = "requirements/REQ-001.md" },
            new() { FilePath = "changes/CHANGE-001.md" },
            new() { FilePath = "prototype/index.html" }
        };

        var filtered = ApplyPrefixFilter(summaries, "requirements/");

        Assert.Single(filtered);
        Assert.Equal("requirements/REQ-001.md", filtered[0].FilePath);
    }

    [Fact]
    public void PrefixFilter_WhenNullPrefix_ReturnsAll()
    {
        var summaries = new List<ArtefactSummaryResponse>
        {
            new() { FilePath = "requirements/REQ-001.md" },
            new() { FilePath = "changes/CHANGE-001.md" },
            new() { FilePath = "prototype/index.html" }
        };

        var filtered = ApplyPrefixFilter(summaries, null);

        Assert.Equal(3, filtered.Count);
    }

    [Fact]
    public void PrefixFilter_WhenEmptyPrefix_ReturnsAll()
    {
        var summaries = new List<ArtefactSummaryResponse>
        {
            new() { FilePath = "requirements/REQ-001.md" },
            new() { FilePath = "changes/CHANGE-001.md" }
        };

        var filtered = ApplyPrefixFilter(summaries, "");

        Assert.Equal(2, filtered.Count);
    }

    private static List<ArtefactSummaryResponse> ApplyPrefixFilter(
        List<ArtefactSummaryResponse> summaries,
        string? prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return summaries;
        }

        return summaries
            .Where(a => a.FilePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
