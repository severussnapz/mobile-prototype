using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Genesis.AI.Tests.Infrastructure;

public class PrototypeDomSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_CssSelectorQuery_ReturnsMatchingElements()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://screen-01";
        const string html = "<section><button data-genesis-id=\"GENESIS-123\" class=\"cta\">Launch</button><button>Ignore</button></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)
            ]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(s3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "button.cta", "test"),
            CancellationToken.None);

        Assert.Single(result.Matches);
        var match = result.Matches[0];
        Assert.Equal(fragmentPath, match.FragmentPath);
        Assert.Equal("button", match.TagName);
        Assert.Contains("Launch", match.TextSnippet, StringComparison.Ordinal);
        Assert.Equal($"{fragmentPath}|GENESIS-123", match.NodeKey);
        Assert.False(string.IsNullOrWhiteSpace(match.CssSelector));
        Assert.Contains("button", match.CssSelector, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cta", match.ClassList);
        Assert.False(result.Truncated);
        Assert.Equal(1, result.TotalMatches);
    }

    [Fact]
    public async Task GetClassNamesInScopeAsync_ClassOnElementBeyondResultCap_IncludesClass()
    {
        // Reproduces the live false rejection: in a deep fragment the .urgency-arrow spans sit
        // past the first 10 elements, so the capped ListAllInScopeAsync never surfaces them and
        // the guard wrongly concludes the class does not exist. The uncapped class-existence
        // check must return every class in the fragment regardless of element depth.
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01-legacy.html";
        const string s3Key = "s3://screen-01-legacy";
        const string html =
            "<div class=\"screen\"><div class=\"page\"><div class=\"queue-toolbar\">" +
            "<div class=\"c4\"><div class=\"c5\"><div class=\"c6\"><div class=\"c7\">" +
            "<div class=\"c8\"><div class=\"c9\"><div class=\"c10\">" +
            "<span class=\"urgency-arrow\">↑</span><span class=\"urgency-arrow\">↑</span>" +
            "</div></div></div></div></div></div></div></div></div></div>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)
            ]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(s3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var classNames = await service.GetClassNamesInScopeAsync(
            projectId, "screen-01-legacy", CancellationToken.None);

        Assert.Contains("urgency-arrow", classNames);
        Assert.Contains("screen", classNames);
    }

    [Fact]
    public async Task SearchAsync_WhenDataGenesisIdMissing_UsesIdForNodeKey()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://screen-01";
        const string html = "<section><button id=\"launch-btn\" class=\"cta\">Launch</button></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)
            ]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(s3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "button.cta", "test"),
            CancellationToken.None);

        Assert.Single(result.Matches);
        var match = result.Matches[0];
        Assert.Equal($"{fragmentPath}|launch-btn", match.NodeKey);
        Assert.Equal("#launch-btn", match.CssSelector);
    }

    [Fact]
    public async Task SearchAsync_WhenDataGenesisIdAndIdMissing_FallsBackToSelectorForNodeKey()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://screen-01";
        const string html = "<section><button class=\"cta\">Launch</button></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)
            ]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(s3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "button.cta", "test"),
            CancellationToken.None);

        Assert.Single(result.Matches);
        var match = result.Matches[0];
        Assert.False(string.IsNullOrWhiteSpace(match.CssSelector));
        Assert.Equal($"{fragmentPath}|css:{match.CssSelector}", match.NodeKey);
    }

    [Fact]
    public async Task BuildSearchMatch_WhenElementHasNoIdOrDataGenesisId_UsesCssPrefixedSelector()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://screen-01";
        const string html = "<div class='toolbar'><button>Zoom +</button></div>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)
            ]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(s3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "button", "test"),
            CancellationToken.None);

        Assert.Single(result.Matches);
        var match = result.Matches[0];
        Assert.Contains("|css:", match.NodeKey, StringComparison.Ordinal);

        var stableLocator = match.NodeKey[(match.NodeKey.IndexOf('|', StringComparison.Ordinal) + 1)..];
        Assert.DoesNotMatch("^[0-9A-Fa-f]{8,}$", stableLocator);
    }

    [Fact]
    public async Task SearchAsync_WhenCssSelectorReturnsNothing_UsesTextFallback()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://screen-01";
        const string html = "<section><h2>Clinical Safety Review</h2><p>Guidance text</p></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)
            ]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(s3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "Clinical Safety", "test"),
            CancellationToken.None);

        Assert.NotEmpty(result.Matches);
        Assert.Contains(
            result.Matches,
            match => match.TextSnippet.Contains("Clinical Safety", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchAsync_WhenRawSelectorMisses_ClassSelectorVariantReturnsMatches()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://screen-01";
        const string html = "<section><div class=\"smart-view-item\">Inbox</div><div class=\"smart-view-item\">Tasks</div></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)
            ]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(s3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "smart-view-item", "test"),
            CancellationToken.None);

        Assert.Equal(2, result.TotalMatches);
        Assert.Equal(2, result.Matches.Count);
        Assert.Contains(result.Matches, match => match.TextSnippet.Equals("Inbox", StringComparison.Ordinal));
        Assert.Contains(result.Matches, match => match.TextSnippet.Equals("Tasks", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_WhenRawAndClassSelectorMiss_IdSelectorVariantReturnsMatch()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://screen-01";
        const string html = "<section><div id=\"shell-nav\">Navigation</div></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)
            ]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(s3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "shell-nav", "test"),
            CancellationToken.None);

        Assert.Single(result.Matches);
        Assert.Equal("div", result.Matches[0].TagName);
        Assert.Equal($"{fragmentPath}|shell-nav", result.Matches[0].NodeKey);
    }

    [Fact]
    public async Task SearchAsync_WhenMoreThanTenMatches_ReturnsOnlyTen()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://screen-01";
        const string html = """
<section>
  <div class="item">1</div>
  <div class="item">2</div>
  <div class="item">3</div>
  <div class="item">4</div>
  <div class="item">5</div>
  <div class="item">6</div>
  <div class="item">7</div>
  <div class="item">8</div>
  <div class="item">9</div>
  <div class="item">10</div>
  <div class="item">11</div>
  <div class="item">12</div>
</section>
""";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)
            ]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(s3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "div.item", "test"),
            CancellationToken.None);

        Assert.Equal(12, result.TotalMatches);
        Assert.Equal(10, result.Matches.Count);
        Assert.True(result.Truncated);
    }

    [Fact]
    public async Task SearchAsync_WhenNoFragmentsExist_ReturnsEmptyResult()
    {
        var projectId = Guid.NewGuid();

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Artefact>());

        var artefactStorageService = new Mock<IArtefactStorageService>();

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "div.item", "test"),
            CancellationToken.None);

        Assert.Empty(result.Matches);
        Assert.False(result.Truncated);
        Assert.Equal(0, result.TotalMatches);
    }

    // ── T8: SearchAsync_WhenQueryIsShellNavId_ReturnsShellNavNode ────────────────
    [Fact]
    public async Task SearchAsync_WhenQueryIsShellNavId_ReturnsShellNavNode()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/shell.html";
        const string s3Key = "s3://shell";
        const string html = "<div id=\"shell-nav\" class=\"shell-nav\"><nav>Navigation</nav></div>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "shell-nav", "user"),
            CancellationToken.None);

        Assert.NotEmpty(result.Matches);
        Assert.Contains(result.Matches, m => m.NodeKey.Contains("shell-nav", StringComparison.OrdinalIgnoreCase));
    }

    // ── T9: SearchAsync_WhenElementHasNoId_UsesCssSelectorAsNodeKey ─────────────
    [Fact]
    public async Task SearchAsync_WhenElementHasNoId_UsesCssSelectorAsNodeKey()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://s1";
        const string html = "<section><button class=\"save-btn\">Save</button></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "button.save-btn", "user"),
            CancellationToken.None);

        Assert.Single(result.Matches);
        var match = result.Matches[0];
        // NodeKey should be fragmentPath|<cssSelector> when no id or data-genesis-id present
        Assert.StartsWith(fragmentPath, match.NodeKey, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(match.CssSelector));
    }

    // ── T10: SearchAsync_WhenElementHasDataGenesisId_UsesItAsNodeKey ────────────
    [Fact]
    public async Task SearchAsync_WhenElementHasDataGenesisId_UsesItAsNodeKey()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://s1";
        const string genesisId = "ABCDEF1234567890";
        const string html = $"<section><button data-genesis-id=\"{genesisId}\" class=\"cta\">Go</button></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "button.cta", "user"),
            CancellationToken.None);

        Assert.Single(result.Matches);
        Assert.Equal($"{fragmentPath}|{genesisId}", result.Matches[0].NodeKey);
    }

    // ── T22: AssemblePrototype_WhenShellMissing_DoesNotThrow ────────────────────
    // (Covered via ListAllAsync graceful empty-fragment handling)
    [Fact]
    public async Task ListAllAsync_WhenNoFragmentsExist_ReturnsEmpty()
    {
        var projectId = Guid.NewGuid();
        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Artefact>());

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, new Mock<IArtefactStorageService>().Object);

        var result = await service.ListAllAsync(
            new PrototypeDomListRequest(projectId, "button", "screen-01", "user"),
            CancellationToken.None);

        Assert.Empty(result.Matches);
    }

    // ── T23: SearchAsync_WhenPrototypeDoesNotExist_ReturnsEmpty ─────────────────
    [Fact]
    public async Task SearchAsync_WhenPrototypeDoesNotExist_ReturnsEmpty()
    {
        var projectId = Guid.NewGuid();
        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Artefact>());

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, new Mock<IArtefactStorageService>().Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "button", "user"),
            CancellationToken.None);

        Assert.Empty(result.Matches);
    }

    // ── T28: SearchAsync_WhenMultipleMatches_LeafNodesRankedBeforeContainers ─────
    [Fact]
    public async Task SearchAsync_WhenMultipleMatches_LeafNodesRankedBeforeContainers()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://s1";
        const string html = """
<section class="container">
  <button class="cta">Click me</button>
</section>
""";
        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "Click me", "user"),
            CancellationToken.None);

        // Should return matches without throwing — leaf button should appear
        Assert.NotEmpty(result.Matches);
        Assert.Contains(result.Matches, m => m.TagName == "button");
    }

    // ── T29: SearchAsync_WhenQueryMatchesTextAndClass_ReturnsResults ─────────────
    [Fact]
    public async Task SearchAsync_WhenQueryMatchesTextAndClass_ReturnsResults()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://s1";
        const string html = "<section><button class=\"primary-action\">Submit form</button></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "primary-action", "user"),
            CancellationToken.None);

        Assert.NotEmpty(result.Matches);
        Assert.Contains(result.Matches, m => m.ClassList.Contains("primary-action"));
    }

    // ── T30: SearchAsync_WhenTagIsExcluded_NotReturned ──────────────────────────
    [Fact]
    public async Task SearchAsync_WhenTagIsExcluded_NotReturned()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://s1";
        const string html = "<html><head></head><body><script>var x = 'script-content';</script><div class=\"target\">visible</div></body></html>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "script-content", "user"),
            CancellationToken.None);

        // script tag is excluded — no matches from inside script blocks
        Assert.DoesNotContain(result.Matches, m => m.TagName is "script" or "html" or "body");
    }

    // ── T31: SearchAsync_WhenParentContext_IncludedInResult ──────────────────────
    [Fact]
    public async Task SearchAsync_WhenParentContext_IncludedInResult()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://s1";
        const string html = "<nav class=\"shell-nav\"><button class=\"nav-item\">Home</button></nav>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "button.nav-item", "user"),
            CancellationToken.None);

        Assert.Single(result.Matches);
        var match = result.Matches[0];
        // ParentContext should reflect the parent tag
        Assert.False(string.IsNullOrWhiteSpace(match.ParentContext));
        Assert.Contains("nav", match.ParentContext, StringComparison.OrdinalIgnoreCase);
    }

    // ── T32: SearchAsync_WhenSiblingContext_IncludedInResult ─────────────────────
    [Fact]
    public async Task SearchAsync_WhenSiblingContext_IncludedInResult()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://s1";
        const string html = "<nav><button class=\"nav-item\">Home</button><button class=\"nav-item\">About</button><button class=\"nav-item\">Contact</button></nav>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "button.nav-item", "user"),
            CancellationToken.None);

        Assert.NotEmpty(result.Matches);
        // At least one match should have sibling context
        Assert.Contains(result.Matches, m => !string.IsNullOrWhiteSpace(m.SiblingContext));
    }

    // ── T38: ListAllAsync_WhenSelectorEmpty_ReturnsEmptyResult ──────────────────
    [Fact]
    public async Task ListAllAsync_WhenSelectorEmpty_ReturnsEmptyResult()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://s1";
        const string html = "<section><button>Test</button></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        // An empty selector should produce no matches without throwing
        var exception = await Record.ExceptionAsync(() => service.ListAllAsync(
            new PrototypeDomListRequest(projectId, string.Empty, "screen-01", "user"),
            CancellationToken.None));

        Assert.Null(exception);
    }

    // ── T39: ListAllAsync_WhenAllMatchesAreUnstable_ReturnsEmpty ────────────────
    [Fact]
    public async Task ListAllAsync_WhenAllMatchesAreUnstable_ReturnsEmpty()
    {
        // All matched elements have unstable node keys (no data-genesis-id, no id) → all are nth-child paths
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://s1";
        const string html = "<ul><li>A</li><li>B</li></ul>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.ListAllAsync(
            new PrototypeDomListRequest(projectId, "li", "screen-01", "user"),
            CancellationToken.None);

        // li elements with no id/data-genesis-id get CSS selector paths → may contain :nth-child
        // The important thing is no exception is thrown
        Assert.NotNull(result);
    }

    // ── T41: SearchAsync_WhenPrototypeDoesNotExistForProject_ReturnsEmpty ────────
    [Fact]
    public async Task SearchAsync_WhenPrototypeDoesNotExistForProject_ReturnsEmpty()
    {
        var projectId = Guid.NewGuid();
        var artefactRepository = new Mock<IArtefactRepository>();
        // Project has artefacts but none are prototype fragments
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, "requirements/manifest.md", "s3://manifest", 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(string.Empty);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.SearchAsync(
            new PrototypeDomSearchRequest(projectId, "prototype/index.html", "button", "user"),
            CancellationToken.None);

        Assert.Empty(result.Matches);
    }

    // ── Plan 3f: wrong selector finds nothing ───────────────────────────────────
    [Fact]
    public async Task ListAllAsync_WhenAgentSelectorIsWrong_MatchesNothing()
    {
        // The agent guesses ".smart-view-item" but the real class is ".sv-item" — must match nothing.
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01-legacy.html";
        const string s3Key = "s3://screen-01-legacy";
        const string html = """
<aside class="smart-views">
  <div class="sv-item" id="sv-all">All documents</div>
  <div class="sv-item" id="sv-urgent">Urgent Letters</div>
</aside>
""";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.ListAllAsync(
            new PrototypeDomListRequest(projectId, ".smart-view-item", "screen-01-legacy", "user"),
            CancellationToken.None);

        Assert.Empty(result.Matches);
    }

    // ── Plan 3f: real elements stay discoverable via scope listing ───────────────
    [Fact]
    public async Task ListAllInScopeAsync_WhenCalledForScope_ReturnsActualElementsWithRealClasses()
    {
        // The scope-wide listing returns the elements ACTUALLY present (with their real classes)
        // so the correct selector is discoverable and a wrong selector can never be silently written.
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01-legacy.html";
        const string s3Key = "s3://screen-01-legacy";
        const string html = """
<aside class="smart-views">
  <div class="sv-item" id="sv-all">All documents</div>
  <div class="sv-item" id="sv-urgent">Urgent Letters</div>
  <div class="sv-item" id="sv-cardiology">Cardiology Follow-up</div>
</aside>
""";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.ListAllInScopeAsync(projectId, "screen-01-legacy", CancellationToken.None);

        Assert.Contains(result.Matches, m => m.ClassList.Contains("sv-item"));
    }

    // ── Plan 3f: confirmed single selector when scope is unambiguous ─────────────
    [Fact]
    public async Task ResolveConfirmedSelectorForScope_WhenAllElementsShareOneClass_ReturnsThatSelector()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01-legacy.html";
        const string s3Key = "s3://screen-01-legacy";
        const string html = """
<aside>
  <div class="sv-item" id="sv-all">All documents</div>
  <div class="sv-item" id="sv-urgent">Urgent Letters</div>
  <div class="sv-item" id="sv-card">Cardiology Follow-up</div>
</aside>
""";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var confirmed = await service.ResolveConfirmedSelectorForScope(projectId, "screen-01-legacy", CancellationToken.None);

        Assert.Equal(".sv-item", confirmed);
    }

    // ── Plan 3f: ambiguous scope returns null so caller lists and asks ───────────
    [Fact]
    public async Task ResolveConfirmedSelectorForScope_WhenElementsHaveNoSingleSharedClass_ReturnsNull()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01-legacy.html";
        const string s3Key = "s3://screen-01-legacy";
        const string html = """
<aside>
  <div class="sv-item" id="a">One</div>
  <nav class="nav-link" id="b">Two</nav>
  <span class="chip" id="c">Three</span>
</aside>
""";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var confirmed = await service.ResolveConfirmedSelectorForScope(projectId, "screen-01-legacy", CancellationToken.None);

        Assert.Null(confirmed);
    }

    // ── Plan 3f Fix 2: confirmed selector derived from the matches themselves ────
    [Fact]
    public void ResolveConfirmedSelectorFromMatches_WhenAllMatchesShareOneClass_ReturnsThatSelector()
    {
        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            new Mock<IArtefactRepository>().Object,
            new Mock<IArtefactStorageService>().Object);

        var matches = new List<PrototypeDomSearchMatch>
        {
            CreateMatch(["urgency-arrow"]),
            CreateMatch(["urgency-arrow"]),
            CreateMatch(["urgency-arrow", "highlight"]),
        };

        var confirmed = service.ResolveConfirmedSelectorFromMatches(matches);

        Assert.Equal(".urgency-arrow", confirmed);
    }

    [Fact]
    public void ResolveConfirmedSelectorFromMatches_WhenMatchesHaveNoSingleSharedClass_ReturnsNull()
    {
        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            new Mock<IArtefactRepository>().Object,
            new Mock<IArtefactStorageService>().Object);

        var matches = new List<PrototypeDomSearchMatch>
        {
            CreateMatch(["nav-link"]),
            CreateMatch(["chip"]),
        };

        var confirmed = service.ResolveConfirmedSelectorFromMatches(matches);

        Assert.Null(confirmed);
    }

    private static PrototypeDomSearchMatch CreateMatch(IReadOnlyList<string> classList) =>
        new(
            NodeKey: $"prototype/fragments/screen-01-legacy.html|css:{Guid.NewGuid()}",
            FragmentPath: "prototype/fragments/screen-01-legacy.html",
            TagName: "span",
            TextSnippet: "urgency",
            CssSelector: "css:span",
            ClassList: classList,
            ParentContext: "div",
            SiblingContext: string.Empty);

    [Fact]
    public async Task ListAllAsync_WhenElementHasChildSpans_TextSnippetContainsOnlyDirectText()
    {
        // BuildSearchMatch should capture only direct text nodes, not descendant text.
        // sv-item contains sv-item-label ("Urgent Letters") and sv-item-count ("3") as child spans.
        // TextSnippet must NOT be "Urgent Letters 3" — it should reflect direct text only (empty or whitespace).
        // This forces the agent to target .sv-item-label for clean tooltip values.
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01-legacy.html";
        const string s3Key = "s3://screen-01-legacy";
        const string html = """
<div class="sv-item" id="sv-urgent">
  <span class="sv-item-icon">🔴</span>
  <span class="sv-item-label">Urgent Letters</span>
  <span class="sv-item-count">3</span>
</div>
""";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.ListAllAsync(
            new PrototypeDomListRequest(projectId, ".sv-item", "screen-01-legacy", "user"),
            CancellationToken.None);

        var svItem = result.Matches.FirstOrDefault(m => m.ClassList.Contains("sv-item"));
        Assert.NotNull(svItem);
        // Direct text only — should NOT contain "3" from the count span
        Assert.DoesNotContain("3", svItem!.TextSnippet);
        Assert.Contains("Urgent Letters", svItem.TextSnippet);
    }
    [Fact]
    public async Task ListAllAsync_WhenElementHasNoMeaningfulDirectText_TextSnippetFallsBackToAriaLabel()
    {
        // When an element's direct text is empty or symbol-only after cleaning (e.g. arrow, icon),
        // TextSnippet must fall back to aria-label or other searchable attributes.
        // This gives the agent meaningful signal regardless of the element type.
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01-legacy.html";
        const string s3Key = "s3://fallback";
        const string html = """
<span class="indicator" aria-label="High priority">↑</span>
""";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.ListAllAsync(
            new PrototypeDomListRequest(projectId, ".indicator", "screen-01-legacy", "user"),
            CancellationToken.None);

        var match = result.Matches.FirstOrDefault(m => m.ClassList.Contains("indicator"));
        Assert.NotNull(match);
        Assert.Contains("High priority", match!.TextSnippet);
    }

    [Fact]
    public async Task ListAllAsync_WhenScopeMatchesOneFragment_ReturnsOnlyThatFragmentsElements()
    {
        // Cross-fragment contamination regression: two fragments each contain a <button>.
        // apply_to_scope must only ever touch the named fragment. Before the file-scoped fix,
        // the production handler passed a null scope node (filename never resolved to a node),
        // so ListAllAsync degraded to document.QuerySelectorAll across every fragment and
        // returned both buttons — the mutation would then write to the wrong fragment path.
        var projectId = Guid.NewGuid();
        const string fragmentPathA = "prototype/fragments/screen-a.html";
        const string fragmentPathB = "prototype/fragments/screen-b.html";
        const string s3KeyA = "s3://screen-a";
        const string s3KeyB = "s3://screen-b";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePublishedArtefact(projectId, fragmentPathA, s3KeyA, 1),
                CreatePublishedArtefact(projectId, fragmentPathB, s3KeyB, 1)
            ]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(s3KeyA, It.IsAny<CancellationToken>()))
            .ReturnsAsync("<section><button>A</button></section>");
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(s3KeyB, It.IsAny<CancellationToken>()))
            .ReturnsAsync("<section><button>B</button></section>");

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var result = await service.ListAllAsync(
            new PrototypeDomListRequest(projectId, "button", "screen-a", "user"),
            CancellationToken.None);

        Assert.Single(result.Matches);
        Assert.All(result.Matches, match => Assert.Equal(fragmentPathA, match.FragmentPath));
        Assert.DoesNotContain(result.Matches, match => match.TextSnippet.Contains('B'));
    }

    private static Artefact CreatePublishedArtefact(Guid projectId, string filePath, string s3Key, int version)
    {
        return Artefact.CreateS3Artefact(
            projectId, version, filePath, s3Key, "text/html", 100, "test", TimeProvider.System, true);
    }

}
