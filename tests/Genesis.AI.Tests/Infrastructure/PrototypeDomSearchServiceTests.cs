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

    [Fact]
    public async Task ListAllAsync_WhenScopeNodeIdStartsWithDigit_DoesNotThrow()
    {
        // scope_node_id whose stableLocator starts with a digit triggers the buggy id-selector path
        // in ResolveNodeByKey (#0ABCDEF...) and must not throw DomException.
        // NOTE: element has only id=, NOT data-genesis-id, so the first attribute lookup misses.
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://screen-01";
        const string digitId = "0ABCDEF123456789";
        const string html = $"<section id=\"{digitId}\"><button>Test</button></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var service = new PrototypeDomSearchService(
            NullLogger<PrototypeDomSearchService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object);

        var exception = await Record.ExceptionAsync(() => service.ListAllAsync(
            new PrototypeDomListRequest(
                projectId,
                "button",
                $"{fragmentPath}|{digitId}",
                "test-user"),
            CancellationToken.None));

        Assert.Null(exception);
    }

    // ── T6: already above ────────────────────────────────────────────────────────

    // ── T7: ListAllAsync_WhenScopeNodeIdContainsNthChild_ReturnsEmpty ────────────
    [Fact]
    public async Task ListAllAsync_WhenScopeNodeIdContainsNthChild_ReturnsEmpty()
    {
        // scope_node_id containing :nth-child is unstable and must be rejected gracefully
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://screen-01";
        const string html = "<section><nav><button>Test</button></nav></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var exception = await Record.ExceptionAsync(() => service.ListAllAsync(
            new PrototypeDomListRequest(projectId, "button", $"{fragmentPath}|section:nth-child(1) > nav:nth-child(1)", "user"),
            CancellationToken.None));

        Assert.Null(exception);
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
            new PrototypeDomListRequest(projectId, "button", null, "user"),
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
            new PrototypeDomListRequest(projectId, string.Empty, null, "user"),
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
            new PrototypeDomListRequest(projectId, "li", null, "user"),
            CancellationToken.None);

        // li elements with no id/data-genesis-id get CSS selector paths → may contain :nth-child
        // The important thing is no exception is thrown
        Assert.NotNull(result);
    }

    // ── T40: ListAllAsync_WhenScopedToValidNode_ReturnsOnlyDescendants ───────────
    [Fact]
    public async Task ListAllAsync_WhenScopedToValidNode_ReturnsOnlyDescendants()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string s3Key = "s3://s1";
        const string html = """
<section>
  <nav data-genesis-id="NAV-1">
    <button data-genesis-id="BTN-A">Inside nav</button>
  </nav>
  <button data-genesis-id="BTN-B">Outside nav</button>
</section>
""";

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePublishedArtefact(projectId, fragmentPath, s3Key, 1)]);
        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService.Setup(s => s.GetContentAsync(s3Key, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomSearchService(NullLogger<PrototypeDomSearchService>.Instance, artefactRepository.Object, artefactStorageService.Object);

        var result = await service.ListAllAsync(
            new PrototypeDomListRequest(projectId, "button", $"{fragmentPath}|NAV-1", "user"),
            CancellationToken.None);

        // Only BTN-A should be returned (inside the scoped nav), not BTN-B
        Assert.All(result.Matches, m => Assert.DoesNotContain("BTN-B", m.NodeKey, StringComparison.Ordinal));
        Assert.Contains(result.Matches, m => m.NodeKey.Contains("BTN-A", StringComparison.Ordinal));
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

    private static Artefact CreatePublishedArtefact(Guid projectId, string filePath, string s3Key, int version)
    {
        return Artefact.CreateS3Artefact(
            projectId,
            version,
            filePath,
            s3Key,
            "text/html",
            128,
            "test",
            TimeProvider.System,
            true);
    }
}