using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetProjects;
using Genesis.AI.Domain.Queries.GetProjectById;
using Moq;

namespace Genesis.AI.Tests.Queries;

public class ProjectQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly TimeProvider _timeProvider;

    public ProjectQueryHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _timeProvider = TimeProvider.System;
    }

    // ========================================================================
    // GetProjectsQueryHandler
    // ========================================================================

    [Fact]
    public async Task GetProjects_NoStatusFilter_ReturnsAllProjects()
    {
        var projects = new List<Project>
        {
            new("DOC", "Documents", null, ComplianceDomain.Generic, "user-1", _timeProvider),
            new("AUTH", "Auth Service", null, ComplianceDomain.ClinicalUk, "user-2", _timeProvider),
        };

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var handler = new GetProjectsQueryHandler(_projectRepositoryMock.Object);
        var query = new GetProjectsQuery(null);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetProjects_WithStatusFilter_CallsGetByStatus()
    {
        var projects = new List<Project>
        {
            new("DOC", "Documents", null, ComplianceDomain.Generic, "user-1", _timeProvider),
        };

        _projectRepositoryMock
            .Setup(r => r.GetByStatusAsync("discovery", It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var handler = new GetProjectsQueryHandler(_projectRepositoryMock.Object);
        var query = new GetProjectsQuery("discovery");

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        _projectRepositoryMock.Verify(r => r.GetByStatusAsync("discovery", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProjects_EmptyStatusFilter_ReturnsAll()
    {
        var projects = new List<Project>
        {
            new("DOC", "Documents", null, ComplianceDomain.Generic, "user-1", _timeProvider),
        };

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var handler = new GetProjectsQueryHandler(_projectRepositoryMock.Object);
        var query = new GetProjectsQuery("   ");

        var result = await handler.Handle(query, CancellationToken.None);

        _projectRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // GetProjectByIdQueryHandler
    // ========================================================================

    [Fact]
    public async Task GetProjectById_Exists_ReturnsProject()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.Generic, "user-1", _timeProvider);
        _projectRepositoryMock
            .Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var handler = new GetProjectByIdQueryHandler(_projectRepositoryMock.Object);
        var query = new GetProjectByIdQuery(project.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(project.Id, result.Id);
        Assert.Equal("Documents", result.Name);
    }

    [Fact]
    public async Task GetProjectById_NotFound_ReturnsNull()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var handler = new GetProjectByIdQueryHandler(_projectRepositoryMock.Object);
        var query = new GetProjectByIdQuery(projectId);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Null(result);
    }
}
