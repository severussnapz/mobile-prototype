using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Infrastructure;

public class PrototypeSearchRouterTests
{
    [Fact]
    public void ShouldRouteToDomSearch_HtmlFragmentPathWithDomModeOn_ReturnsTrue()
    {
        var result = PrototypeSearchRouter.ShouldRouteToDomSearch(
            "prototype/fragments/screen-01-legacy.html", domModeEnabled: true);

        Assert.True(result);
    }

    [Fact]
    public void ShouldRouteToDomSearch_AssembledIndexHtmlWithDomModeOn_ReturnsTrue()
    {
        var result = PrototypeSearchRouter.ShouldRouteToDomSearch(
            "prototype/index.html", domModeEnabled: true);

        Assert.True(result);
    }

    [Fact]
    public void ShouldRouteToDomSearch_StylesCssFragment_ReturnsFalse()
    {
        var result = PrototypeSearchRouter.ShouldRouteToDomSearch(
            "prototype/fragments/_styles.css", domModeEnabled: true);

        Assert.False(result);
    }

    [Fact]
    public void ShouldRouteToDomSearch_DataJsFragment_ReturnsFalse()
    {
        var result = PrototypeSearchRouter.ShouldRouteToDomSearch(
            "prototype/fragments/data.js", domModeEnabled: true);

        Assert.False(result);
    }

    [Fact]
    public void ShouldRouteToDomSearch_DomModeDisabled_ReturnsFalse()
    {
        var result = PrototypeSearchRouter.ShouldRouteToDomSearch(
            "prototype/fragments/screen-01-legacy.html", domModeEnabled: false);

        Assert.False(result);
    }

    [Fact]
    public void ShouldRouteToDomSearch_NonPrototypeHtml_ReturnsFalse()
    {
        var result = PrototypeSearchRouter.ShouldRouteToDomSearch(
            "requirements/REQ-001.md", domModeEnabled: true);

        Assert.False(result);
    }
}
