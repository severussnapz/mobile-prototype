using Genesis.AI.Api.Features.Conversations;

namespace Genesis.AI.Tests.Infrastructure;

/// <summary>
/// Tests for ConversationStreamController.BuildGetArtefactResult — the get_artefact
/// result builder that decides between full content, a structural outline, and an
/// already-read pointer. Prototype HTML fragments are returned in full (the outline
/// is a CSS digest that misreads markup-heavy fragments) and are guarded against
/// repeated full reads within a single request.
/// </summary>
public class ConversationStreamControllerGetArtefactTests
{
    private const int LargeFileThreshold = 50_000;

    [Fact]
    public void BuildGetArtefactResult_LargePrototypeHtmlFragment_ReturnsFullContentNotOutline()
    {
        var content = "<div class=\"screen\" id=\"screen-checkout\">" + new string('a', 60_000) + "</div>";

        var result = ConversationStreamController.BuildGetArtefactResult(
            "prototype/fragments/screen-01-legacy.html",
            content,
            version: 3,
            alreadyReadThisRequest: false,
            largeFileThreshold: LargeFileThreshold);

        Assert.DoesNotContain("STRUCTURAL OUTLINE", result, StringComparison.Ordinal);
        Assert.Contains("id=\"screen-checkout\"", result, StringComparison.Ordinal);
        Assert.Contains(content, result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildGetArtefactResult_LargeNonPrototypeHtmlFile_ReturnsStructuralOutline()
    {
        var content =
            ":root { --color-primary: #ffffff; }\n" +
            ".card { color: red; }\n" +
            "<!-- HEADER SECTION -->\n" +
            new string('b', 60_000);

        var result = ConversationStreamController.BuildGetArtefactResult(
            "design/old-mockup.html",
            content,
            version: 1,
            alreadyReadThisRequest: false,
            largeFileThreshold: LargeFileThreshold);

        Assert.Contains("STRUCTURAL OUTLINE", result, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('b', 60_000), result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildGetArtefactResult_LargePrototypeIndexSingleFileMode_ReturnsFullContent()
    {
        var content = "<!DOCTYPE html><html><body>" + new string('x', 60_000) + "</body></html>";

        var result = ConversationStreamController.BuildGetArtefactResult(
            "prototype/index.html",
            content,
            version: 7,
            alreadyReadThisRequest: false,
            largeFileThreshold: LargeFileThreshold,
            prototypeSingleFile: true);

        Assert.DoesNotContain("STRUCTURAL OUTLINE", result, StringComparison.Ordinal);
        Assert.Contains(content, result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildGetArtefactResult_PrototypeHtmlFragmentAlreadyRead_ReturnsPointerNotContent()
    {
        var content = "<div class=\"screen\" id=\"screen-checkout\">" + new string('a', 60_000) + "</div>";

        var result = ConversationStreamController.BuildGetArtefactResult(
            "prototype/fragments/screen-01-legacy.html",
            content,
            version: 4,
            alreadyReadThisRequest: true,
            largeFileThreshold: LargeFileThreshold);

        Assert.Contains("ALREADY READ", result, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"screen-checkout\"", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildGetArtefactResult_SmallPrototypeFragmentFirstRead_ReturnsFullContent()
    {
        const string content = "<div class=\"screen\" id=\"screen-home\">Hello</div>";

        var result = ConversationStreamController.BuildGetArtefactResult(
            "prototype/fragments/screen-02-home.html",
            content,
            version: 2,
            alreadyReadThisRequest: false,
            largeFileThreshold: LargeFileThreshold);

        Assert.DoesNotContain("ALREADY READ", result, StringComparison.Ordinal);
        Assert.DoesNotContain("STRUCTURAL OUTLINE", result, StringComparison.Ordinal);
        Assert.Contains(content, result, StringComparison.Ordinal);
    }
}
