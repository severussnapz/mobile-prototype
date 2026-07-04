using System.Security.Claims;
using Genesis.AI.Api.Features.Artefacts;
using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetArtefactVersions;
using JsonApi.Resources.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Tests.Features.Artefacts;

public class ArtefactVersionControllerTests
{
    private const string PrototypePath = "prototype/index.html";

    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock = new();
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private ArtefactVersionController CreateController()
    {
        _artefactRepositoryMock.SetupGet(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);

        var controller = new ArtefactVersionController(
            _mediatorMock.Object,
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _timeProvider)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        return controller;
    }

    [Fact]
    public async Task GetVersionsByFilePath_WhenDatabaseEmptyForPrototype_FallsBackToS3()
    {
        var projectId = Guid.NewGuid();
        _mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<GetArtefactVersionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetArtefactVersionsResult([]));
        _artefactStorageServiceMock
            .Setup(storage => storage.ListVersionsAsync(projectId, PrototypePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(int, long, DateTimeOffset)>
            {
                (2, 200, DateTimeOffset.UtcNow),
                (1, 100, DateTimeOffset.UtcNow)
            });
        var controller = CreateController();

        var result = await controller.GetVersionsByFilePath(
            projectId, PrototypePath, new PaginationFilter { Page = 1, Size = 20 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var versions = Assert.IsAssignableFrom<IReadOnlyList<ArtefactVersionResponse>>(ok.Value);
        Assert.Equal(2, versions.Count);
        Assert.Equal(2, versions[0].Version);
        Assert.Equal("system", versions[0].CreatedBy);
        Assert.Equal("text/html", versions[0].ContentType);
    }

    [Fact]
    public async Task GetVersionsByFilePath_WhenDatabaseEmptyForNonPrototype_ReturnsEmptyWithoutS3()
    {
        var projectId = Guid.NewGuid();
        _mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<GetArtefactVersionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetArtefactVersionsResult([]));
        var controller = CreateController();

        var result = await controller.GetVersionsByFilePath(
            projectId, "requirements/REQ-001.md", new PaginationFilter { Page = 1, Size = 20 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var versions = Assert.IsAssignableFrom<IReadOnlyList<ArtefactVersionResponse>>(ok.Value);
        Assert.Empty(versions);
        _artefactStorageServiceMock.Verify(
            storage => storage.ListVersionsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RestoreByBody_WhenVersionNotInDatabaseForPrototype_RestoresFromS3()
    {
        var projectId = Guid.NewGuid();
        var s3Key = $"projects/{projectId}/artefacts/{PrototypePath}/v3";
        _artefactRepositoryMock
            .Setup(repository => repository.GetVersionsByFilePathAsync(projectId, PrototypePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact>());
        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync(s3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>v3</html>");
        _artefactRepositoryMock
            .Setup(repository => repository.GetNextVersionForFileAsync(projectId, PrototypePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                projectId, PrototypePath, 5, "<html>v3</html>", "text/html", It.IsAny<CancellationToken>()))
            .ReturnsAsync($"projects/{projectId}/artefacts/{PrototypePath}/v5");
        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var controller = CreateController();

        var result = await controller.RestoreByBody(
            projectId, new RestoreArtefactVersionRequest { FilePath = PrototypePath, Version = 3 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var summary = Assert.IsType<ArtefactSummaryResponse>(ok.Value);
        Assert.Equal(5, summary.Version);
        Assert.Equal(PrototypePath, summary.FilePath);
        _artefactRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreByBody_WhenVersionNotInDatabaseForNonPrototype_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        _artefactRepositoryMock
            .Setup(repository => repository.GetVersionsByFilePathAsync(
                projectId, "requirements/REQ-001.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact>());
        var controller = CreateController();

        var result = await controller.RestoreByBody(
            projectId,
            new RestoreArtefactVersionRequest { FilePath = "requirements/REQ-001.md", Version = 3 },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
