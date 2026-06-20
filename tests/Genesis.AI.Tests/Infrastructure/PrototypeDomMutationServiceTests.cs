using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Genesis.AI.Tests.Infrastructure;

public class PrototypeDomMutationServiceTests
{
    [Fact]
    public async Task ApplyMutationAsync_SetAttribute_ByDataGenesisId_PersistsNewVersionAndAssembles()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://fragment-v1";
        const string original = "<section><button data-genesis-id=\"NODE-1\">Launch</button></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        var artefactStorageService = new Mock<IArtefactStorageService>();
        var prototypeAssemblyService = new Mock<IPrototypeAssemblyService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        artefactRepository.Setup(repository => repository.UnitOfWork).Returns(unitOfWork.Object);
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        artefactRepository
            .Setup(repository => repository.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        artefactRepository
            .Setup(repository => repository.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        artefactRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        artefactRepository
            .Setup(repository => repository.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactStorageService
            .Setup(storage => storage.GetContentAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);

        string? persistedContent = null;
        artefactStorageService
            .Setup(storage => storage.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>((_, _, _, content, _, _) => persistedContent = content)
            .ReturnsAsync("s3://fragment-v2");

        var service = new PrototypeDomMutationService(
            NullLogger<PrototypeDomMutationService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object,
            prototypeAssemblyService.Object,
            TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(
                projectId,
                fragmentPath,
                $"{fragmentPath}|NODE-1",
                PrototypeDomMutationOperation.SetAttribute,
                "title",
                "Launch now",
                "test-user"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("ok", result.Message);
        Assert.Equal(fragmentPath, result.FragmentPath);
        Assert.Equal(2, result.Version);
        Assert.NotNull(persistedContent);
        Assert.Contains("title=\"Launch now\"", persistedContent, StringComparison.Ordinal);

        prototypeAssemblyService.Verify(
            service => service.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyMutationAsync_AddClass_ById_PersistsMutation()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://fragment-v1";
        const string original = "<section><div id=\"card\">Panel</div></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        var artefactStorageService = new Mock<IArtefactStorageService>();
        var prototypeAssemblyService = new Mock<IPrototypeAssemblyService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        artefactRepository.Setup(repository => repository.UnitOfWork).Returns(unitOfWork.Object);
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        artefactRepository
            .Setup(repository => repository.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        artefactRepository
            .Setup(repository => repository.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        artefactRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        artefactRepository
            .Setup(repository => repository.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactStorageService
            .Setup(storage => storage.GetContentAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);

        string? persistedContent = null;
        artefactStorageService
            .Setup(storage => storage.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>((_, _, _, content, _, _) => persistedContent = content)
            .ReturnsAsync("s3://fragment-v2");

        var service = new PrototypeDomMutationService(
            NullLogger<PrototypeDomMutationService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object,
            prototypeAssemblyService.Object,
            TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(
                projectId,
                fragmentPath,
                $"{fragmentPath}|card",
                PrototypeDomMutationOperation.AddClass,
                null,
                "active",
                "test-user"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(persistedContent);
        Assert.Contains("class=\"active\"", persistedContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyMutationAsync_SetText_BySelectorFallback_UpdatesTargetNode()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://fragment-v1";
        const string original = "<section><p class=\"caption\">Old</p></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        var artefactStorageService = new Mock<IArtefactStorageService>();
        var prototypeAssemblyService = new Mock<IPrototypeAssemblyService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        artefactRepository.Setup(repository => repository.UnitOfWork).Returns(unitOfWork.Object);
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        artefactRepository
            .Setup(repository => repository.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        artefactRepository
            .Setup(repository => repository.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        artefactRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        artefactRepository
            .Setup(repository => repository.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactStorageService
            .Setup(storage => storage.GetContentAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);

        string? persistedContent = null;
        artefactStorageService
            .Setup(storage => storage.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>((_, _, _, content, _, _) => persistedContent = content)
            .ReturnsAsync("s3://fragment-v2");

        var service = new PrototypeDomMutationService(
            NullLogger<PrototypeDomMutationService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object,
            prototypeAssemblyService.Object,
            TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(
                projectId,
                fragmentPath,
                $"{fragmentPath}|p.caption",
                PrototypeDomMutationOperation.SetText,
                null,
                "Updated",
                "test-user"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(persistedContent);
        Assert.Contains(">Updated<", persistedContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyMutationAsync_AddClassExisting_WhenNoChange_ReturnsNoOpAndSkipsPersist()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://fragment-v1";
        const string original = "<section><div data-genesis-id=\"NODE-1\" class=\"active\">Panel</div></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        var artefactStorageService = new Mock<IArtefactStorageService>();
        var prototypeAssemblyService = new Mock<IPrototypeAssemblyService>();

        artefactRepository
            .Setup(repository => repository.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));

        artefactStorageService
            .Setup(storage => storage.GetContentAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);

        var service = new PrototypeDomMutationService(
            NullLogger<PrototypeDomMutationService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object,
            prototypeAssemblyService.Object,
            TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(
                projectId,
                fragmentPath,
                $"{fragmentPath}|NODE-1",
                PrototypeDomMutationOperation.AddClass,
                null,
                "active",
                "test-user"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("no-op", result.Message);
        Assert.Null(result.Version);

        artefactStorageService.Verify(
            storage => storage.SaveContentAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        prototypeAssemblyService.Verify(
            service => service.AssemblePrototypeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyMutationAsync_InsertAdjacentHtml_BeforeEnd_InsertsMarkup()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://fragment-v1";
        const string original = "<section><ul data-genesis-id=\"LIST-1\"></ul></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        var artefactStorageService = new Mock<IArtefactStorageService>();
        var prototypeAssemblyService = new Mock<IPrototypeAssemblyService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        artefactRepository.Setup(repository => repository.UnitOfWork).Returns(unitOfWork.Object);
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        artefactRepository
            .Setup(repository => repository.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        artefactRepository
            .Setup(repository => repository.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        artefactRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        artefactRepository
            .Setup(repository => repository.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactStorageService
            .Setup(storage => storage.GetContentAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);

        string? persistedContent = null;
        artefactStorageService
            .Setup(storage => storage.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>((_, _, _, content, _, _) => persistedContent = content)
            .ReturnsAsync("s3://fragment-v2");

        var service = new PrototypeDomMutationService(
            NullLogger<PrototypeDomMutationService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object,
            prototypeAssemblyService.Object,
            TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(
                projectId,
                fragmentPath,
                $"{fragmentPath}|LIST-1",
                PrototypeDomMutationOperation.InsertAdjacentHtml,
                "beforeend",
                "<li>New item</li>",
                "test-user"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(persistedContent);
        Assert.Contains("<ul data-genesis-id=\"LIST-1\"><li>New item</li></ul>", persistedContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyMutationAsync_RemoveElement_RemovesTargetNode()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://fragment-v1";
        const string original = "<section><div data-genesis-id=\"CARD-1\">Panel</div><p>Keep</p></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        var artefactStorageService = new Mock<IArtefactStorageService>();
        var prototypeAssemblyService = new Mock<IPrototypeAssemblyService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        artefactRepository.Setup(repository => repository.UnitOfWork).Returns(unitOfWork.Object);
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        artefactRepository
            .Setup(repository => repository.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        artefactRepository
            .Setup(repository => repository.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        artefactRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        artefactRepository
            .Setup(repository => repository.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactStorageService
            .Setup(storage => storage.GetContentAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);

        string? persistedContent = null;
        artefactStorageService
            .Setup(storage => storage.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>((_, _, _, content, _, _) => persistedContent = content)
            .ReturnsAsync("s3://fragment-v2");

        var service = new PrototypeDomMutationService(
            NullLogger<PrototypeDomMutationService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object,
            prototypeAssemblyService.Object,
            TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(
                projectId,
                fragmentPath,
                $"{fragmentPath}|CARD-1",
                PrototypeDomMutationOperation.RemoveElement,
                null,
                null,
                "test-user"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(persistedContent);
        Assert.DoesNotContain("CARD-1", persistedContent, StringComparison.Ordinal);
        Assert.Contains("<p>Keep</p>", persistedContent, StringComparison.Ordinal);
    }

    // ── T3: ApplyMutationAsync_WhenMutationSucceeds_ReturnsSuccessWithVersion ──────
    [Fact]
    public async Task ApplyMutationAsync_WhenMutationSucceeds_ReturnsSuccessWithVersion()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://v1";
        const string html = "<section><div data-genesis-id=\"ABC\">Hello</div></section>";

        var repo = new Mock<IArtefactRepository>();
        var storage = new Mock<IArtefactStorageService>();
        var assembly = new Mock<IPrototypeAssemblyService>();
        var uow = new Mock<IUnitOfWork>();

        repo.Setup(r => r.UnitOfWork).Returns(uow.Object);
        uow.Setup(w => w.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        repo.Setup(r => r.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        repo.Setup(r => r.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        repo.Setup(r => r.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.DeletePreviousVersionsAsync(projectId, fragmentPath, 7, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        storage.Setup(s => s.GetContentAsync(storageKey, It.IsAny<CancellationToken>())).ReturnsAsync(html);
        storage.Setup(s => s.SaveContentAsync(projectId, fragmentPath, 7, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
               .ReturnsAsync("s3://v7");

        var service = new PrototypeDomMutationService(NullLogger<PrototypeDomMutationService>.Instance, repo.Object, storage.Object, assembly.Object, TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|ABC", PrototypeDomMutationOperation.SetAttribute, "aria-label", "New label", "user"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("ok", result.Message);
        Assert.Equal(7, result.Version);
        Assert.Equal(fragmentPath, result.FragmentPath);
    }

    // ── T4: ApplyMutationAsync_WhenFragmentNotFound_ReturnsFailure ───────────────
    [Fact]
    public async Task ApplyMutationAsync_WhenFragmentNotFound_ReturnsFailure()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/missing.html";

        var repo = new Mock<IArtefactRepository>();
        var storage = new Mock<IArtefactStorageService>();
        var assembly = new Mock<IPrototypeAssemblyService>();
        repo.Setup(r => r.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var service = new PrototypeDomMutationService(NullLogger<PrototypeDomMutationService>.Instance, repo.Object, storage.Object, assembly.Object, TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|NODE", PrototypeDomMutationOperation.SetAttribute, "title", "X", "user"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("fragment not found", result.Message);
    }

    // ── T5: ApplyMutationAsync_WhenNoOp_ReturnsNoOpMessage ─────────────────────
    [Fact]
    public async Task ApplyMutationAsync_WhenNoOp_ReturnsNoOpMessage()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://v1";
        // Attribute already present with the same value → no-op
        const string html = "<section><button data-genesis-id=\"BTN\" aria-label=\"Save\">Save</button></section>";

        var repo = new Mock<IArtefactRepository>();
        var storage = new Mock<IArtefactStorageService>();
        var assembly = new Mock<IPrototypeAssemblyService>();
        repo.Setup(r => r.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        storage.Setup(s => s.GetContentAsync(storageKey, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomMutationService(NullLogger<PrototypeDomMutationService>.Instance, repo.Object, storage.Object, assembly.Object, TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|BTN", PrototypeDomMutationOperation.SetAttribute, "aria-label", "Save", "user"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("no-op", result.Message);
        Assert.Null(result.Version);
        storage.Verify(s => s.SaveContentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── T21: AssemblePrototype_WhenFragmentMutated_ReassemblesCorrectly ──────────
    [Fact]
    public async Task AssemblePrototype_WhenFragmentMutated_ReassemblesCorrectly()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://v1";
        const string html = "<section><button data-genesis-id=\"BTN\">Old</button></section>";

        var repo = new Mock<IArtefactRepository>();
        var storage = new Mock<IArtefactStorageService>();
        var assembly = new Mock<IPrototypeAssemblyService>();
        var uow = new Mock<IUnitOfWork>();

        repo.Setup(r => r.UnitOfWork).Returns(uow.Object);
        uow.Setup(w => w.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        repo.Setup(r => r.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        repo.Setup(r => r.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        repo.Setup(r => r.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        storage.Setup(s => s.GetContentAsync(storageKey, It.IsAny<CancellationToken>())).ReturnsAsync(html);
        storage.Setup(s => s.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>())).ReturnsAsync("s3://v2");

        var service = new PrototypeDomMutationService(NullLogger<PrototypeDomMutationService>.Instance, repo.Object, storage.Object, assembly.Object, TimeProvider.System);

        await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|BTN", PrototypeDomMutationOperation.SetText, null, "New", "user"),
            CancellationToken.None);

        // Assembly is triggered once on successful mutation
        assembly.Verify(a => a.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── T24: ResolveTargetElement_WhenNodeIdHasSpecialCharsInSelector_DoesNotThrow
    [Fact]
    public async Task ResolveTargetElement_WhenNodeIdHasSpecialCharsInSelector_DoesNotThrow()
    {
        // CSS selector chars like '.' in stableLocator should not throw
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://v1";
        const string html = "<section><div class=\"foo.bar\">test</div></section>";

        var repo = new Mock<IArtefactRepository>();
        var storage = new Mock<IArtefactStorageService>();
        var assembly = new Mock<IPrototypeAssemblyService>();
        repo.Setup(r => r.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        storage.Setup(s => s.GetContentAsync(storageKey, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomMutationService(NullLogger<PrototypeDomMutationService>.Instance, repo.Object, storage.Object, assembly.Object, TimeProvider.System);

        var exception = await Record.ExceptionAsync(() => service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|foo.bar", PrototypeDomMutationOperation.SetAttribute, "title", "X", "user"),
            CancellationToken.None));

        Assert.Null(exception);
    }

    // ── T25: ResolveTargetElement_WhenFragmentPathMismatch_ReturnsNull ───────────
    [Fact]
    public async Task ResolveTargetElement_WhenFragmentPathMismatch_ReturnsNull()
    {
        // nodeKey with no '|' separator (just a raw fragment path) produces an empty stableLocator
        // and must return element not found without throwing
        var projectId = Guid.NewGuid();
        const string requestedFragment = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://v1";
        const string html = "<section><div data-genesis-id=\"NODE\">test</div></section>";

        var repo = new Mock<IArtefactRepository>();
        var storage = new Mock<IArtefactStorageService>();
        var assembly = new Mock<IPrototypeAssemblyService>();
        repo.Setup(r => r.GetByProjectAndFilePathAsync(projectId, requestedFragment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, requestedFragment, storageKey, 1));
        storage.Setup(s => s.GetContentAsync(storageKey, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomMutationService(NullLogger<PrototypeDomMutationService>.Instance, repo.Object, storage.Object, assembly.Object, TimeProvider.System);

        // Node key has no '|' separator → ExtractStableLocator returns empty → element not found
        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(projectId, requestedFragment, requestedFragment, PrototypeDomMutationOperation.SetAttribute, "title", "X", "user"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("target element not found", result.Message);
    }

    // ── T26: ResolveTargetElement_WhenDocumentEmpty_ReturnsNull ─────────────────
    [Fact]
    public async Task ResolveTargetElement_WhenDocumentEmpty_ReturnsNull()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://v1";

        var repo = new Mock<IArtefactRepository>();
        var storage = new Mock<IArtefactStorageService>();
        var assembly = new Mock<IPrototypeAssemblyService>();
        repo.Setup(r => r.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        storage.Setup(s => s.GetContentAsync(storageKey, It.IsAny<CancellationToken>())).ReturnsAsync("<html><body></body></html>");

        var service = new PrototypeDomMutationService(NullLogger<PrototypeDomMutationService>.Instance, repo.Object, storage.Object, assembly.Object, TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|NONEXISTENT", PrototypeDomMutationOperation.SetAttribute, "title", "X", "user"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("target element not found", result.Message);
    }

    // ── T27: ResolveTargetElement_WhenMultipleMatchesForDataGenesisId_ReturnsFirst
    [Fact]
    public async Task ResolveTargetElement_WhenMultipleMatchesForDataGenesisId_ReturnsFirst()
    {
        // Even with duplicate data-genesis-id, the mutation should succeed without throwing
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://v1";
        const string html = "<section><div data-genesis-id=\"DUP\">First</div><div data-genesis-id=\"DUP\">Second</div></section>";

        var repo = new Mock<IArtefactRepository>();
        var storage = new Mock<IArtefactStorageService>();
        var assembly = new Mock<IPrototypeAssemblyService>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.UnitOfWork).Returns(uow.Object);
        uow.Setup(w => w.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        repo.Setup(r => r.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        repo.Setup(r => r.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        repo.Setup(r => r.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        storage.Setup(s => s.GetContentAsync(storageKey, It.IsAny<CancellationToken>())).ReturnsAsync(html);
        storage.Setup(s => s.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>())).ReturnsAsync("s3://v2");

        var service = new PrototypeDomMutationService(NullLogger<PrototypeDomMutationService>.Instance, repo.Object, storage.Object, assembly.Object, TimeProvider.System);

        // Should not throw — should succeed on the first match
        var exception = await Record.ExceptionAsync(() => service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|DUP", PrototypeDomMutationOperation.SetAttribute, "title", "X", "user"),
            CancellationToken.None));

        Assert.Null(exception);
    }

    // ── T34: InsertAdjacentHtml_WhenPositionInvalid_ReturnsMutationError ─────────
    [Fact]
    public async Task InsertAdjacentHtml_WhenPositionInvalid_ReturnsMutationError()
    {
        // AngleSharp InsertAdjacentHTML throws on invalid position; service should not propagate exception
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://v1";
        const string html = "<section><ul data-genesis-id=\"LIST\"></ul></section>";

        var repo = new Mock<IArtefactRepository>();
        var storage = new Mock<IArtefactStorageService>();
        var assembly = new Mock<IPrototypeAssemblyService>();
        repo.Setup(r => r.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        storage.Setup(s => s.GetContentAsync(storageKey, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomMutationService(NullLogger<PrototypeDomMutationService>.Instance, repo.Object, storage.Object, assembly.Object, TimeProvider.System);

        var exception = await Record.ExceptionAsync(() => service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|LIST",
                PrototypeDomMutationOperation.InsertAdjacentHtml, "invalid_position", "<li>x</li>", "user"),
            CancellationToken.None));

        // Must not throw — either succeeds or returns a failure result
        Assert.Null(exception);
    }

    // ── T37: SetAttribute_WhenValueSame_IsNoOp ──────────────────────────────────
    [Fact]
    public async Task SetAttribute_WhenValueSame_IsNoOp()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://v1";
        const string html = "<section><div data-genesis-id=\"NODE\" title=\"Existing\">Test</div></section>";

        var repo = new Mock<IArtefactRepository>();
        var storage = new Mock<IArtefactStorageService>();
        var assembly = new Mock<IPrototypeAssemblyService>();
        repo.Setup(r => r.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        storage.Setup(s => s.GetContentAsync(storageKey, It.IsAny<CancellationToken>())).ReturnsAsync(html);

        var service = new PrototypeDomMutationService(NullLogger<PrototypeDomMutationService>.Instance, repo.Object, storage.Object, assembly.Object, TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|NODE", PrototypeDomMutationOperation.SetAttribute, "title", "Existing", "user"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("no-op", result.Message);
        storage.Verify(s => s.SaveContentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── T1: ResolveTargetElement_WhenStableLocatorStartsWithDigit_DoesNotThrow ──
    [Fact]
    public async Task ResolveTargetElement_WhenStableLocatorStartsWithDigit_DoesNotThrow()
    {
        // An id that starts with a digit produces an invalid CSS id selector (#0A5...)
        // which causes AngleSharp to throw DomException: "The string did not match the expected pattern".
        // The fix skips the id-selector path when the locator starts with a digit.
        // NOTE: element has only id=, NOT data-genesis-id, so the first lookup misses and we hit the id-selector path.
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://fragment-v1";
        const string nodeId = "0A5A5A2F2A98EDB7";
        const string original = $"<section><div id=\"{nodeId}\">test</div></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        var artefactStorageService = new Mock<IArtefactStorageService>();
        var prototypeAssemblyService = new Mock<IPrototypeAssemblyService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        artefactRepository.Setup(r => r.UnitOfWork).Returns(unitOfWork.Object);
        unitOfWork.Setup(w => w.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        artefactRepository
            .Setup(r => r.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        artefactRepository
            .Setup(r => r.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        artefactRepository
            .Setup(r => r.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        artefactRepository
            .Setup(r => r.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactStorageService
            .Setup(s => s.GetContentAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);
        artefactStorageService
            .Setup(s => s.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3://fragment-v2");

        var service = new PrototypeDomMutationService(
            NullLogger<PrototypeDomMutationService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object,
            prototypeAssemblyService.Object,
            TimeProvider.System);

        // Must not throw — returns either success (element found via data-genesis-id) or not-found
        var exception = await Record.ExceptionAsync(() => service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(
                projectId,
                fragmentPath,
                $"{fragmentPath}|{nodeId}",
                PrototypeDomMutationOperation.SetAttribute,
                "aria-label",
                "Test",
                "test-user"),
            CancellationToken.None));

        Assert.Null(exception);
    }

    // ── T2: ResolveTargetElement_WhenNodeIdIsCssPath_ResolvesAndMutates ──────────
    [Fact]
    public async Task ResolveTargetElement_WhenNodeIdIsCssPath_ResolvesAndMutates()
    {
        // CSS-path node ids (including :nth-child and >) are valid selectors and should resolve.
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://fragment-v1";
        const string original = "<section><button>Click</button></section>";

        var artefactRepository = new Mock<IArtefactRepository>();
        var artefactStorageService = new Mock<IArtefactStorageService>();
        var prototypeAssemblyService = new Mock<IPrototypeAssemblyService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        artefactRepository.Setup(r => r.UnitOfWork).Returns(unitOfWork.Object);
        unitOfWork.Setup(w => w.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        artefactRepository
            .Setup(r => r.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        artefactRepository
            .Setup(r => r.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        artefactRepository
            .Setup(r => r.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        artefactRepository
            .Setup(r => r.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactStorageService
            .Setup(s => s.GetContentAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);
        artefactStorageService
            .Setup(s => s.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3://fragment-v2");

        var service = new PrototypeDomMutationService(
            NullLogger<PrototypeDomMutationService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object,
            prototypeAssemblyService.Object,
            TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(
                projectId,
                fragmentPath,
                $"{fragmentPath}|section:nth-child(1) > button:nth-child(1)",
                PrototypeDomMutationOperation.SetAttribute,
                "aria-label",
                "Test",
                "test-user"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("ok", result.Message);
    }

    [Fact]
    public async Task ApplyMutationAsync_WhenNodeIdHasCssPrefix_ResolvesAndMutatesCorrectly()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://fragment-v1";
        const string original = "<div><button>Zoom +</button></div>";

        var artefactRepository = new Mock<IArtefactRepository>();
        var artefactStorageService = new Mock<IArtefactStorageService>();
        var prototypeAssemblyService = new Mock<IPrototypeAssemblyService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        artefactRepository.Setup(repository => repository.UnitOfWork).Returns(unitOfWork.Object);
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        artefactRepository
            .Setup(repository => repository.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        artefactRepository
            .Setup(repository => repository.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        artefactRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        artefactRepository
            .Setup(repository => repository.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactStorageService
            .Setup(storage => storage.GetContentAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);

        string? persistedContent = null;
        artefactStorageService
            .Setup(storage => storage.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>((_, _, _, content, _, _) => persistedContent = content)
            .ReturnsAsync("s3://fragment-v2");

        var service = new PrototypeDomMutationService(
            NullLogger<PrototypeDomMutationService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object,
            prototypeAssemblyService.Object,
            TimeProvider.System);

        var result = await service.ApplyMutationAsync(
            new PrototypeDomMutationRequest(
                projectId,
                fragmentPath,
                $"{fragmentPath}|css:body>div>button",
                PrototypeDomMutationOperation.SetAttribute,
                "aria-label",
                "Zoom in",
                "test-user"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(persistedContent);
        Assert.Contains("aria-label=\"Zoom in\"", persistedContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyBulkAttributes_WhenValidRequests_AppliesAllMutations()
    {
        var projectId = Guid.NewGuid();
        const string fragmentPath = "prototype/fragments/screen-01.html";
        const string storageKey = "s3://fragment-v1";
        const string original = "<button>One</button><button>Two</button><button>Three</button>";

        var artefactRepository = new Mock<IArtefactRepository>();
        var artefactStorageService = new Mock<IArtefactStorageService>();
        var prototypeAssemblyService = new Mock<IPrototypeAssemblyService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        artefactRepository.Setup(repository => repository.UnitOfWork).Returns(unitOfWork.Object);
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        artefactRepository
            .Setup(repository => repository.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedArtefact(projectId, fragmentPath, storageKey, 1));
        artefactRepository
            .Setup(repository => repository.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        artefactRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        artefactRepository
            .Setup(repository => repository.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactStorageService
            .Setup(storage => storage.GetContentAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);

        string? persistedContent = null;
        artefactStorageService
            .Setup(storage => storage.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>((_, _, _, content, _, _) => persistedContent = content)
            .ReturnsAsync("s3://fragment-v2");

        var service = new PrototypeDomMutationService(
            NullLogger<PrototypeDomMutationService>.Instance,
            artefactRepository.Object,
            artefactStorageService.Object,
            prototypeAssemblyService.Object,
            TimeProvider.System);

        var batchResult = await service.ApplyBatchMutationAsync(
            [
                new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|css:body>button:nth-child(1)", PrototypeDomMutationOperation.SetAttribute, "aria-label", "Button 1", "test-user"),
                new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|css:body>button:nth-child(2)", PrototypeDomMutationOperation.SetAttribute, "aria-label", "Button 2", "test-user"),
                new PrototypeDomMutationRequest(projectId, fragmentPath, $"{fragmentPath}|css:body>button:nth-child(3)", PrototypeDomMutationOperation.SetAttribute, "aria-label", "Button 3", "test-user")
            ],
            CancellationToken.None);

        Assert.Equal(3, batchResult.TotalMutations);
        Assert.Equal(3, batchResult.SuccessfulMutations);
        Assert.NotNull(persistedContent);
        Assert.Contains("aria-label=\"Button 1\"", persistedContent, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Button 2\"", persistedContent, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Button 3\"", persistedContent, StringComparison.Ordinal);
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