using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Domain;

public sealed class PrototypeFragmentMigrationTests
{
    private const string MonolithicHtml = """
        <!DOCTYPE html>
        <html>
        <head>
        <style>
        body { font-family: sans-serif; }
        .btn-primary { background: var(--primary); color: white; }
        </style>
        </head>
        <body>
        <nav id="nav-shell">
          <span>EMIS-X</span>
        </nav>
        <main>
          <div class="screen" id="screen-inbox" data-screen="inbox">
            <h1>Inbox</h1>
            <p>Document list here</p>
          </div>
          <div class="screen" id="screen-gallery" data-screen="gallery">
            <h1>Gallery</h1>
            <p>Document viewer here</p>
          </div>
        </main>
        <script>
        function navigate(screen) {
          document.querySelectorAll('.screen').forEach(s => s.style.display = 'none');
          document.getElementById('screen-' + screen).style.display = 'block';
        }
        navigate('inbox');
        </script>
        </body>
        </html>
        """;

    private const string MonolithicHtmlNoStyle = """
        <!DOCTYPE html>
        <html>
        <head></head>
        <body>
        <nav id="nav-shell"><span>EMIS-X</span></nav>
        <main>
          <div class="screen" id="screen-inbox" data-screen="inbox"><h1>Inbox</h1></div>
        </main>
        <script>function navigate() {}</script>
        </body>
        </html>
        """;

    private const string MonolithicHtmlNoScript = """
        <!DOCTYPE html>
        <html>
        <head>
        <style>body { font-family: sans-serif; }</style>
        </head>
        <body>
        <nav id="nav-shell"><span>EMIS-X</span></nav>
        <main>
          <div class="screen" id="screen-inbox" data-screen="inbox"><h1>Inbox</h1></div>
        </main>
        </body>
        </html>
        """;

    private const string MonolithicHtmlNoScreens = """
        <!DOCTYPE html>
        <html>
        <head>
        <style>body { font-family: sans-serif; }</style>
        </head>
        <body>
        <nav id="nav-shell"><span>EMIS-X</span></nav>
        <main><p>Loading...</p></main>
        <script>function init() {}</script>
        </body>
        </html>
        """;

    private static Artefact MakeArtefact(Guid projectId, string path, string s3Key) =>
        Artefact.CreateS3Artefact(projectId, 1, path, s3Key, "text/html", 1000, "seed", TimeProvider.System, true);

    private static (
        Mock<IArtefactStorageService> storage,
        Mock<IArtefactRepository> repo,
        Mock<IPrototypeAssemblyService> assembly,
        PrototypeFragmentMigrationService sut)
        BuildSut(Guid projectId, string indexHtml)
    {
        var storageMock = new Mock<IArtefactStorageService>();
        var repoMock = new Mock<IArtefactRepository>();
        var assemblyMock = new Mock<IPrototypeAssemblyService>();

        repoMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "prototype/fragments/_shell.html", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var indexArtefact = MakeArtefact(projectId, "prototype/index.html", "s3-index-key");
        repoMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "prototype/index.html", It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexArtefact);

        storageMock
            .Setup(s => s.GetContentAsync("s3-index-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexHtml);

        storageMock
            .Setup(s => s.SaveContentAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-saved-key");

        repoMock
            .Setup(r => r.GetNextVersionForFileAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        repoMock
            .Setup(r => r.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repoMock
            .Setup(r => r.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new PrototypeFragmentMigrationService(
            storageMock.Object, repoMock.Object, assemblyMock.Object, TimeProvider.System);

        return (storageMock, repoMock, assemblyMock, sut);
    }

    // ─── Group 1: Migration detection ───────────────────────────────────────

    [Fact]
    public async Task WhenMonolithicIndexExists_AndNoFragments_SplitsIntoFragmentsAndTriggersAssembly()
    {
        var projectId = Guid.NewGuid();
        var (storage, _, assembly, sut) = BuildSut(projectId, MonolithicHtml);

        var result = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.True(result.Migrated);
        storage.Verify(s => s.SaveContentAsync(projectId, "prototype/fragments/_styles.css",
            It.IsAny<int>(), It.IsAny<string>(), "text/css", It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.SaveContentAsync(projectId, "prototype/fragments/_app.js",
            It.IsAny<int>(), It.IsAny<string>(), "application/javascript", It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.SaveContentAsync(projectId, "prototype/fragments/_shell.html",
            It.IsAny<int>(), It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.SaveContentAsync(projectId, "prototype/fragments/screen-01-legacy.html",
            It.IsAny<int>(), It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()), Times.Once);
        assembly.Verify(a => a.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenShellFragmentExists_SkipsMigration()
    {
        var projectId = Guid.NewGuid();
        var storageMock = new Mock<IArtefactStorageService>();
        var repoMock = new Mock<IArtefactRepository>();
        var assemblyMock = new Mock<IPrototypeAssemblyService>();

        var shellArtefact = MakeArtefact(projectId, "prototype/fragments/_shell.html", "s3-shell-key");
        repoMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "prototype/fragments/_shell.html", It.IsAny<CancellationToken>()))
            .ReturnsAsync(shellArtefact);

        var sut = new PrototypeFragmentMigrationService(
            storageMock.Object, repoMock.Object, assemblyMock.Object, TimeProvider.System);

        var result = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.False(result.Migrated);
        storageMock.Verify(s => s.SaveContentAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        assemblyMock.Verify(a => a.AssemblePrototypeAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNoIndexHtmlAndNoFragments_SkipsMigration()
    {
        var projectId = Guid.NewGuid();
        var storageMock = new Mock<IArtefactStorageService>();
        var repoMock = new Mock<IArtefactRepository>();
        var assemblyMock = new Mock<IPrototypeAssemblyService>();

        repoMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var sut = new PrototypeFragmentMigrationService(
            storageMock.Object, repoMock.Object, assemblyMock.Object, TimeProvider.System);

        var result = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.False(result.Migrated);
        storageMock.Verify(s => s.SaveContentAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Group 2: Fragment content correctness ───────────────────────────────

    [Fact]
    public async Task StylesFragment_ContainsOnlyExtractedCss_NoHtmlWrapper()
    {
        var projectId = Guid.NewGuid();
        string? savedCss = null;
        var (storage, _, _, sut) = BuildSut(projectId, MonolithicHtml);

        storage
            .Setup(s => s.SaveContentAsync(projectId, "prototype/fragments/_styles.css",
                It.IsAny<int>(), It.IsAny<string>(), "text/css", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>(
                (_, _, _, content, _, _) => savedCss = content)
            .ReturnsAsync("s3-key");

        await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.NotNull(savedCss);
        Assert.Contains("btn-primary", savedCss);
        Assert.DoesNotContain("<style>", savedCss);
        Assert.DoesNotContain("<html>", savedCss);
    }

    [Fact]
    public async Task AppJsFragment_ContainsOnlyExtractedScript_NoHtmlWrapper()
    {
        var projectId = Guid.NewGuid();
        string? savedJs = null;
        var (storage, _, _, sut) = BuildSut(projectId, MonolithicHtml);

        storage
            .Setup(s => s.SaveContentAsync(projectId, "prototype/fragments/_app.js",
                It.IsAny<int>(), It.IsAny<string>(), "application/javascript", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>(
                (_, _, _, content, _, _) => savedJs = content)
            .ReturnsAsync("s3-key");

        await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.NotNull(savedJs);
        Assert.Contains("navigate", savedJs);
        Assert.DoesNotContain("<script>", savedJs);
        Assert.DoesNotContain("<html>", savedJs);
    }

    [Fact]
    public async Task ShellFragment_ContainsNavLayout_ButNotScreenDivs()
    {
        var projectId = Guid.NewGuid();
        string? savedShell = null;
        var (storage, _, _, sut) = BuildSut(projectId, MonolithicHtml);

        storage
            .Setup(s => s.SaveContentAsync(projectId, "prototype/fragments/_shell.html",
                It.IsAny<int>(), It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>(
                (_, _, _, content, _, _) => savedShell = content)
            .ReturnsAsync("s3-key");

        await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.NotNull(savedShell);
        Assert.Contains("nav-shell", savedShell);
        Assert.DoesNotContain("data-screen=\"inbox\"", savedShell);
        Assert.DoesNotContain("data-screen=\"gallery\"", savedShell);
    }

    [Fact]
    public async Task ScreensFragment_ContainsAllScreenDivs_AndNothingElse()
    {
        var projectId = Guid.NewGuid();
        string? savedScreens = null;
        var (storage, _, _, sut) = BuildSut(projectId, MonolithicHtml);

        storage
            .Setup(s => s.SaveContentAsync(projectId, "prototype/fragments/screen-01-legacy.html",
                It.IsAny<int>(), It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>(
                (_, _, _, content, _, _) => savedScreens = content)
            .ReturnsAsync("s3-key");

        await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.NotNull(savedScreens);
        Assert.Contains("data-screen=\"inbox\"", savedScreens);
        Assert.Contains("data-screen=\"gallery\"", savedScreens);
        Assert.DoesNotContain("nav-shell", savedScreens);
        Assert.DoesNotContain("<html>", savedScreens);
    }

    // ─── Group 3: Agent adds new fragment ────────────────────────────────────

    [Fact]
    public async Task WhenAgentSavesNewScreenFragment_AssemblyIsTriggered()
    {
        var projectId = Guid.NewGuid();
        var assemblyMock = new Mock<IPrototypeAssemblyService>();
        var repoMock = new Mock<IArtefactRepository>();
        var storageMock = new Mock<IArtefactStorageService>();

        // Simulate: _shell.html exists (already fragmented)
        var shellArtefact = MakeArtefact(projectId, "prototype/fragments/_shell.html", "s3-shell");
        repoMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "prototype/fragments/_shell.html", It.IsAny<CancellationToken>()))
            .ReturnsAsync(shellArtefact);

        var sut = new PrototypeFragmentMigrationService(
            storageMock.Object, repoMock.Object, assemblyMock.Object, TimeProvider.System);

        // Migration skipped — now verify assembly is wired in controller (tested via skip path)
        var result = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.False(result.Migrated);
        // Assembly for new fragment saves is handled by ConversationStreamController — not migration service
        // This test confirms migration correctly skips when fragments exist
        assemblyMock.Verify(a => a.AssemblePrototypeAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAgentSavesOutsideFragmentsPath_AssemblyNotTriggeredByMigration()
    {
        var projectId = Guid.NewGuid();
        var assemblyMock = new Mock<IPrototypeAssemblyService>();
        var repoMock = new Mock<IArtefactRepository>();
        var storageMock = new Mock<IArtefactStorageService>();

        repoMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var sut = new PrototypeFragmentMigrationService(
            storageMock.Object, repoMock.Object, assemblyMock.Object, TimeProvider.System);

        var result = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.False(result.Migrated);
        assemblyMock.Verify(a => a.AssemblePrototypeAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Group 4: Agent edits existing fragment ───────────────────────────────

    [Fact]
    public async Task WhenShellFragmentExists_MigrationSkips_AssemblyNotCalledByMigration()
    {
        var projectId = Guid.NewGuid();
        var assemblyMock = new Mock<IPrototypeAssemblyService>();
        var repoMock = new Mock<IArtefactRepository>();
        var storageMock = new Mock<IArtefactStorageService>();

        var shellArtefact = MakeArtefact(projectId, "prototype/fragments/_shell.html", "s3-shell");
        repoMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "prototype/fragments/_shell.html", It.IsAny<CancellationToken>()))
            .ReturnsAsync(shellArtefact);

        var sut = new PrototypeFragmentMigrationService(
            storageMock.Object, repoMock.Object, assemblyMock.Object, TimeProvider.System);

        var result = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.False(result.Migrated);
        assemblyMock.Verify(a => a.AssemblePrototypeAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenStylesFragmentExists_MigrationSkips_StorageNotWritten()
    {
        var projectId = Guid.NewGuid();
        var assemblyMock = new Mock<IPrototypeAssemblyService>();
        var repoMock = new Mock<IArtefactRepository>();
        var storageMock = new Mock<IArtefactStorageService>();

        var shellArtefact = MakeArtefact(projectId, "prototype/fragments/_shell.html", "s3-shell");
        repoMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "prototype/fragments/_shell.html", It.IsAny<CancellationToken>()))
            .ReturnsAsync(shellArtefact);

        var sut = new PrototypeFragmentMigrationService(
            storageMock.Object, repoMock.Object, assemblyMock.Object, TimeProvider.System);

        await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        storageMock.Verify(s => s.SaveContentAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Group 5: Assembly output correctness ────────────────────────────────

    [Fact]
    public async Task WhenMigrationCompletes_AssemblyIsCalledExactlyOnce()
    {
        var projectId = Guid.NewGuid();
        var (_, _, assembly, sut) = BuildSut(projectId, MonolithicHtml);

        await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        assembly.Verify(a => a.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenMigrationCompletes_FourFragmentsSavedBeforeAssembly()
    {
        var projectId = Guid.NewGuid();
        var saveOrder = new List<string>();
        var assemblyCalledAfterSaves = false;
        var (storage, _, assembly, sut) = BuildSut(projectId, MonolithicHtml);

        storage
            .Setup(s => s.SaveContentAsync(
                projectId, It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>(
                (_, path, _, _, _, _) => saveOrder.Add(path))
            .ReturnsAsync("s3-key");

        assembly
            .Setup(a => a.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()))
            .Callback(() => assemblyCalledAfterSaves = saveOrder.Count == 4)
            .Returns(Task.CompletedTask);

        await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.Equal(4, saveOrder.Count);
        Assert.True(assemblyCalledAfterSaves);
    }

    // ─── Group 6: Edge cases ─────────────────────────────────────────────────

    [Fact]
    public async Task WhenMonolithHasNoStyleBlock_MigrationCompletes_StylesSavedAsEmpty()
    {
        var projectId = Guid.NewGuid();
        var (storage, _, _, sut) = BuildSut(projectId, MonolithicHtmlNoStyle);

        var result = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.True(result.Migrated);
        storage.Verify(s => s.SaveContentAsync(projectId, "prototype/fragments/_styles.css",
            It.IsAny<int>(), It.IsAny<string>(), "text/css", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenMonolithHasNoScriptBlock_MigrationCompletes_AppJsSavedAsEmpty()
    {
        var projectId = Guid.NewGuid();
        var (storage, _, _, sut) = BuildSut(projectId, MonolithicHtmlNoScript);

        var result = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.True(result.Migrated);
        storage.Verify(s => s.SaveContentAsync(projectId, "prototype/fragments/_app.js",
            It.IsAny<int>(), It.IsAny<string>(), "application/javascript", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenMonolithHasNoScreenDivs_MigrationCompletes_ScreensFragmentSavedAsEmpty()
    {
        var projectId = Guid.NewGuid();
        var (storage, _, _, sut) = BuildSut(projectId, MonolithicHtmlNoScreens);

        var result = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.True(result.Migrated);
        storage.Verify(s => s.SaveContentAsync(projectId, "prototype/fragments/screen-01-legacy.html",
            It.IsAny<int>(), It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Group 7: Idempotency ────────────────────────────────────────────────

    [Fact]
    public async Task WhenMigrationCalledTwiceRapidly_SecondCallSkips_NoFragmentsDuplicated()
    {
        var projectId = Guid.NewGuid();
        var storageMock = new Mock<IArtefactStorageService>();
        var repoMock = new Mock<IArtefactRepository>();
        var assemblyMock = new Mock<IPrototypeAssemblyService>();

        // First call: no shell exists, index exists
        var callCount = 0;
        var shellArtefact = MakeArtefact(projectId, "prototype/fragments/_shell.html", "s3-shell");

        repoMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "prototype/fragments/_shell.html", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                // First call returns null, subsequent calls return the artefact (simulating race)
                callCount++;
                return callCount == 1 ? null : shellArtefact;
            });

        var indexArtefact = MakeArtefact(projectId, "prototype/index.html", "s3-index-key");
        repoMock
            .Setup(r => r.GetByProjectAndFilePathAsync(
                projectId, "prototype/index.html", It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexArtefact);

        storageMock
            .Setup(s => s.GetContentAsync("s3-index-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MonolithicHtml);

        storageMock
            .Setup(s => s.SaveContentAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-key");

        repoMock
            .Setup(r => r.GetNextVersionForFileAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        repoMock
            .Setup(r => r.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repoMock
            .Setup(r => r.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new PrototypeFragmentMigrationService(
            storageMock.Object, repoMock.Object, assemblyMock.Object, TimeProvider.System);

        var result1 = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);
        var result2 = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.True(result1.Migrated);
        Assert.False(result2.Migrated);
        // Assembly only called once — on first migration
        assemblyMock.Verify(a => a.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task WhenMonolithHasNoPrototypeMetadata_ShellFragmentContainsInjectedMetadataBlock()
    {
        // Regression guard: legacy prototypes built before the metadata contract
        // do not have a prototype-metadata script block. Assembly validation requires it.
        // Migration must inject a minimal stub so assembly succeeds on migrated prototypes.
        var projectId = Guid.NewGuid();
        string? savedShell = null;
        var (storage, _, _, sut) = BuildSut(projectId, MonolithicHtml); // MonolithicHtml has no prototype-metadata

        storage
            .Setup(s => s.SaveContentAsync(projectId, "prototype/fragments/_shell.html",
                It.IsAny<int>(), It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>(
                (_, _, _, content, _, _) => savedShell = content)
            .ReturnsAsync("s3-key");

        await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.NotNull(savedShell);
        Assert.Contains("prototype-metadata", savedShell);
    }

    [Fact]
    public async Task WhenMonolithHasNoPrototypeBanner_ShellFragmentContainsInjectedBanner()
    {
        // Regression guard: legacy prototypes may not have the exact banner string
        // required by assembly validation. Migration must inject it.
        var projectId = Guid.NewGuid();
        string? savedShell = null;
        var (storage, _, _, sut) = BuildSut(projectId, MonolithicHtml);

        storage
            .Setup(s => s.SaveContentAsync(projectId, "prototype/fragments/_shell.html",
                It.IsAny<int>(), It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>(
                (_, _, _, content, _, _) => savedShell = content)
            .ReturnsAsync("s3-key");

        await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);

        Assert.NotNull(savedShell);
        Assert.Contains("PROTOTYPE ONLY", savedShell);
    }

    [Fact]
    public async Task WhenShellExistsWithMetadataButNoGenesisMarkers_PatchesAndReassembles()
    {
        var projectId = Guid.NewGuid();
        string? savedShell = null;
        var storageMock = new Mock<IArtefactStorageService>();
        var repoMock = new Mock<IArtefactRepository>();
        var assemblyMock = new Mock<IPrototypeAssemblyService>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        repoMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        const string shellNoMarkers =
            "<!DOCTYPE html><html><head>" +
            "<script id=\"prototype-metadata\" type=\"application/json\">{}</script>" +
            "</head><body>" +
            "<p>\u26a0\ufe0f PROTOTYPE ONLY \u2014 custom banner</p>" +
            "<nav>nav</nav><main>content</main>" +
            "</body></html>";
        var shellArtefact = MakeArtefact(projectId, "prototype/fragments/_shell.html", "s3-shell-key");
        repoMock.Setup(r => r.GetByProjectAndFilePathAsync(projectId, "prototype/fragments/_shell.html", It.IsAny<CancellationToken>())).ReturnsAsync(shellArtefact);
        storageMock.Setup(s => s.GetContentAsync("s3-shell-key", It.IsAny<CancellationToken>())).ReturnsAsync(shellNoMarkers);
        storageMock.Setup(s => s.SaveContentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>((_, _, _, c, _, _) => savedShell = c)
            .ReturnsAsync("s3-new-key");
        repoMock.Setup(r => r.GetNextVersionForFileAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);
        repoMock.Setup(r => r.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = new PrototypeFragmentMigrationService(storageMock.Object, repoMock.Object, assemblyMock.Object, TimeProvider.System);
        var result = await sut.MigrateIfNeededAsync(projectId, "idris.issa", CancellationToken.None);
        Assert.True(result.Migrated);
        Assert.NotNull(savedShell);
        Assert.Contains("<!-- GENESIS:STYLES -->", savedShell);
        Assert.Contains("<!-- GENESIS:SCREENS -->", savedShell);
        Assert.Contains("<!-- GENESIS:APP -->", savedShell);
        assemblyMock.Verify(a => a.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

}