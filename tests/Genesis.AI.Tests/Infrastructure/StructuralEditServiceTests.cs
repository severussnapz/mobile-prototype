using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Moq;

namespace Genesis.AI.Tests.Infrastructure;

public class StructuralEditServiceTests
{
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock;
    private readonly Mock<IPrototypeAssemblyService> _prototypeAssemblyServiceMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IRequirementsFeedbackLoopService> _requirementsFeedbackLoopServiceMock;
    private readonly TimeProvider _timeProvider;
    private readonly StructuralEditService _service;

    public StructuralEditServiceTests()
    {
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        _prototypeAssemblyServiceMock = new Mock<IPrototypeAssemblyService>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _requirementsFeedbackLoopServiceMock = new Mock<IRequirementsFeedbackLoopService>();
        _timeProvider = TimeProvider.System;

        _artefactRepositoryMock.Setup(repo => repo.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _prototypeAssemblyServiceMock
            .Setup(service => service.AssemblePrototypeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var draftService = new StructuralEditDraftService(
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _projectRepositoryMock.Object,
            _requirementsFeedbackLoopServiceMock.Object,
            _timeProvider);

        var reorderService = new StructuralEditReorderService(
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _prototypeAssemblyServiceMock.Object,
            draftService);

        var mutationService = new StructuralEditMutationService(
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _prototypeAssemblyServiceMock.Object,
            draftService);

        _service = new StructuralEditService(reorderService, mutationService);
    }

    [Fact]
    public async Task ApplyAsync_DuplicateOperation_UsesDraftPromotionAndNormalisesRootSectionId()
    {
        var projectId = Guid.NewGuid();
        var sourcePath = "prototype/fragments/screen-01-dashboard.html";
        var sourceContent = "<section id=\"old-id\"><h2>Dashboard</h2></section>";
        var sourceArtefact = Artefact.CreateS3Artefact(projectId, 1, sourcePath, "s3-source", "text/html", sourceContent.Length, "seed", _timeProvider, true);
        var existing = new List<Artefact> { sourceArtefact };
        Artefact? addedDraft = null;
        bool? wasDraftPublishedOnAdd = null;
        var savedPayload = string.Empty;

        _artefactRepositoryMock
            .Setup(repo => repo.GetByProjectAndFilePathAsync(projectId, sourcePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceArtefact);
        _artefactRepositoryMock
            .Setup(repo => repo.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceContent);
        _artefactRepositoryMock
            .Setup(repo => repo.GetNextVersionForFileAsync(projectId, "prototype/fragments/screen-02-dashboard-copy.html", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                projectId,
                "prototype/fragments/screen-02-dashboard-copy.html",
                1,
                It.IsAny<string>(),
                "text/html",
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, int, string, string, CancellationToken>((_, _, _, content, _, _) => savedPayload = content)
            .ReturnsAsync("s3-draft");

        _artefactRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Callback<Artefact, CancellationToken>((artefact, _) =>
            {
                wasDraftPublishedOnAdd = artefact.IsPublished;
                addedDraft = artefact;
                existing.Add(artefact);
            })
            .Returns(Task.CompletedTask);

        _artefactRepositoryMock
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => existing.FirstOrDefault(artefact => artefact.Id == id));

        _artefactRepositoryMock
            .Setup(repo => repo.DeletePreviousVersionsAsync(projectId, "prototype/fragments/screen-02-dashboard-copy.html", 1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ApplyAsync(
            projectId,
            new StructuralEditRequest("duplicate", sourcePath, null, null),
            "tester",
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(addedDraft);
        Assert.False(wasDraftPublishedOnAdd);
        Assert.True(savedPayload.Contains("id=\"screen-02-dashboard-copy\"", StringComparison.Ordinal));
        _prototypeAssemblyServiceMock.Verify(service => service.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_ToggleVisibility_UsesDraftPromotionPath()
    {
        var projectId = Guid.NewGuid();
        var fragmentPath = "prototype/fragments/screen-01-dashboard.html";
        var sourceContent = "<section id=\"screen-01-dashboard\"><h2>Dashboard</h2></section>";
        var sourceArtefact = Artefact.CreateS3Artefact(projectId, 1, fragmentPath, "s3-source", "text/html", sourceContent.Length, "seed", _timeProvider, true);
        var tracked = new Dictionary<Guid, Artefact>();
        bool? wasDraftPublishedOnAdd = null;

        _artefactRepositoryMock
            .Setup(repo => repo.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceArtefact);
        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceContent);
        _artefactRepositoryMock
            .Setup(repo => repo.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-draft");

        _artefactRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Callback<Artefact, CancellationToken>((artefact, _) =>
            {
                wasDraftPublishedOnAdd = artefact.IsPublished;
                tracked[artefact.Id] = artefact;
            })
            .Returns(Task.CompletedTask);

        _artefactRepositoryMock
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => tracked.GetValueOrDefault(id));

        _artefactRepositoryMock
            .Setup(repo => repo.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ApplyAsync(
            projectId,
            new StructuralEditRequest("toggle_visibility", fragmentPath, null, true),
            "tester",
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.False(wasDraftPublishedOnAdd);
        _prototypeAssemblyServiceMock.Verify(service => service.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_DuplicateOperation_InvalidCopiedContent_FailsValidationBeforePromotion()
    {
        var projectId = Guid.NewGuid();
        var sourcePath = "prototype/fragments/screen-01-dashboard.html";
        var invalidContent = "<section id=\"old-id\"><div <span>broken</span></section>";
        var sourceArtefact = Artefact.CreateS3Artefact(projectId, 1, sourcePath, "s3-source", "text/html", invalidContent.Length, "seed", _timeProvider, true);
        var existing = new List<Artefact> { sourceArtefact };

        _artefactRepositoryMock
            .Setup(repo => repo.GetByProjectAndFilePathAsync(projectId, sourcePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceArtefact);
        _artefactRepositoryMock
            .Setup(repo => repo.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidContent);

        var result = await _service.ApplyAsync(
            projectId,
            new StructuralEditRequest("duplicate", sourcePath, null, null),
            "tester",
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("malformed tag boundaries", result.Message, StringComparison.OrdinalIgnoreCase);

        _artefactStorageServiceMock.Verify(
            storage => storage.SaveContentAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _artefactRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()), Times.Never);
        _artefactRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _artefactRepositoryMock.Verify(repo => repo.DeletePreviousVersionsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _artefactRepositoryMock.Verify(repo => repo.DeleteByIdAsync(sourceArtefact.Id, It.IsAny<CancellationToken>()), Times.Never);
        _artefactStorageServiceMock.Verify(storage => storage.DeleteContentAsync("s3-source", It.IsAny<CancellationToken>()), Times.Never);
        _prototypeAssemblyServiceMock.Verify(service => service.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_ReorderOperation_ValidationFailure_DiscardsDraftAndKeepsPublishedUntouched()
    {
        var projectId = Guid.NewGuid();
        var firstPath = "prototype/fragments/screen-01-alpha.html";
        var secondPath = "prototype/fragments/screen-02-beta.html";
        var firstContent = "<section id=\"screen-01-alpha\"><h2>Alpha</h2></section>";
        var secondInvalidContent = "<section id=\"screen-02-beta\"><div <span>broken</span></section>";

        var firstArtefact = Artefact.CreateS3Artefact(projectId, 1, firstPath, "s3-first", "text/html", firstContent.Length, "seed", _timeProvider, true);
        var secondArtefact = Artefact.CreateS3Artefact(projectId, 1, secondPath, "s3-second", "text/html", secondInvalidContent.Length, "seed", _timeProvider, true);

        var published = new List<Artefact> { firstArtefact, secondArtefact };
        var draftsById = new Dictionary<Guid, Artefact>();

        _artefactRepositoryMock
            .Setup(repo => repo.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(published);

        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-first", It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstContent);
        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-second", It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondInvalidContent);

        _artefactRepositoryMock
            .Setup(repo => repo.GetNextVersionForFileAsync(projectId, firstPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(projectId, firstPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-draft-first");

        _artefactRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Callback<Artefact, CancellationToken>((artefact, _) => draftsById[artefact.Id] = artefact)
            .Returns(Task.CompletedTask);

        _artefactRepositoryMock
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => draftsById.GetValueOrDefault(id));

        var result = await _service.ApplyAsync(
            projectId,
            new StructuralEditRequest("reorder", null, [firstPath, secondPath], null),
            "tester",
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("malformed tag boundaries", result.Message, StringComparison.OrdinalIgnoreCase);

        _artefactStorageServiceMock.Verify(storage => storage.SaveContentAsync(projectId, firstPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()), Times.Once);
        _artefactStorageServiceMock.Verify(storage => storage.DeleteContentAsync("s3-draft-first", It.IsAny<CancellationToken>()), Times.Once);

        _artefactRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _artefactRepositoryMock.Verify(repo => repo.DeleteByIdAsync(It.Is<Guid>(id => id == firstArtefact.Id || id == secondArtefact.Id), It.IsAny<CancellationToken>()), Times.Never);
        _artefactStorageServiceMock.Verify(storage => storage.DeleteContentAsync("s3-first", It.IsAny<CancellationToken>()), Times.Never);
        _artefactStorageServiceMock.Verify(storage => storage.DeleteContentAsync("s3-second", It.IsAny<CancellationToken>()), Times.Never);
        _artefactRepositoryMock.Verify(repo => repo.DeletePreviousVersionsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _prototypeAssemblyServiceMock.Verify(service => service.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_DeleteOperation_CreatesBackupDraftAndDiscardsAfterSuccessfulDeletion()
    {
        var projectId = Guid.NewGuid();
        var fragmentPath = "prototype/fragments/screen-01-dashboard.html";
        var sourceContent = "<section id=\"screen-01-dashboard\"><h2>Dashboard</h2></section>";
        var sourceArtefact = Artefact.CreateS3Artefact(projectId, 1, fragmentPath, "s3-source", "text/html", sourceContent.Length, "seed", _timeProvider, true);
        var tracked = new Dictionary<Guid, Artefact>();

        _artefactRepositoryMock
            .Setup(repo => repo.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceArtefact);
        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceContent);
        _artefactRepositoryMock
            .Setup(repo => repo.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-backup-draft");

        _artefactRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Callback<Artefact, CancellationToken>((artefact, _) => tracked[artefact.Id] = artefact)
            .Returns(Task.CompletedTask);

        _artefactRepositoryMock
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => tracked.GetValueOrDefault(id));

        var result = await _service.ApplyAsync(
            projectId,
            new StructuralEditRequest("delete", fragmentPath, null, null),
            "tester",
            CancellationToken.None);

        Assert.True(result.Success);

        _artefactStorageServiceMock.Verify(storage => storage.DeleteContentAsync("s3-source", It.IsAny<CancellationToken>()), Times.Once);
        _artefactRepositoryMock.Verify(repo => repo.DeleteByIdAsync(sourceArtefact.Id, It.IsAny<CancellationToken>()), Times.Once);
        _artefactStorageServiceMock.Verify(storage => storage.DeleteContentAsync("s3-backup-draft", It.IsAny<CancellationToken>()), Times.Once);
        _prototypeAssemblyServiceMock.Verify(service => service.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_DeleteOperation_DeleteFailure_PromotesBackupDraftAndRestoresPublishedState()
    {
        var projectId = Guid.NewGuid();
        var fragmentPath = "prototype/fragments/screen-01-dashboard.html";
        var sourceContent = "<section id=\"screen-01-dashboard\"><h2>Dashboard</h2></section>";
        var sourceArtefact = Artefact.CreateS3Artefact(projectId, 1, fragmentPath, "s3-source", "text/html", sourceContent.Length, "seed", _timeProvider, true);
        var tracked = new Dictionary<Guid, Artefact>();

        _artefactRepositoryMock
            .Setup(repo => repo.GetByProjectAndFilePathAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceArtefact);
        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceContent);
        _artefactRepositoryMock
            .Setup(repo => repo.GetNextVersionForFileAsync(projectId, fragmentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(projectId, fragmentPath, 2, It.IsAny<string>(), "text/html", It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-backup-draft");

        _artefactRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Callback<Artefact, CancellationToken>((artefact, _) => tracked[artefact.Id] = artefact)
            .Returns(Task.CompletedTask);

        _artefactRepositoryMock
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => tracked.GetValueOrDefault(id));

        _artefactStorageServiceMock
            .Setup(storage => storage.DeleteContentAsync("s3-source", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated delete failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApplyAsync(
                projectId,
                new StructuralEditRequest("delete", fragmentPath, null, null),
                "tester",
                CancellationToken.None));

        _artefactRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _artefactRepositoryMock.Verify(repo => repo.DeletePreviousVersionsAsync(projectId, fragmentPath, 2, It.IsAny<CancellationToken>()), Times.Once);

        _artefactRepositoryMock.Verify(repo => repo.DeleteByIdAsync(sourceArtefact.Id, It.IsAny<CancellationToken>()), Times.Never);
        _artefactStorageServiceMock.Verify(storage => storage.DeleteContentAsync("s3-backup-draft", It.IsAny<CancellationToken>()), Times.Never);
        _prototypeAssemblyServiceMock.Verify(service => service.AssemblePrototypeAsync(projectId, It.IsAny<CancellationToken>()), Times.Never);
    }
}
