using Genesis.AI.Api.Features.Export;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Tests.Api.Export;

public class ProjectExportControllerErrorTests
{
    [Fact]
    public async Task ExportProject_WhenS3ReadFails_Returns500WithUserMessage()
    {
        var timeProvider = TimeProvider.System;
        var project = new Project(
            code: "EXP1",
            name: "Export Test",
            description: "desc",
            timeSheetCode: "PORTASK0001045",
            complianceDomain: ComplianceDomain.Generic,
            createdBy: "tester@example.com",
            timeProvider: timeProvider);

        var artefact = Artefact.CreateS3Artefact(
            project.Id,
            version: 1,
            filePath: "requirements/REQ-001.md",
            s3Key: "projects/test/artefacts/requirements/REQ-001.md/v1",
            contentType: "text/markdown",
            sizeBytes: 128,
            createdBy: "tester@example.com",
            timeProvider: timeProvider,
            isPublished: true);

        var projectRepository = new Mock<IProjectRepository>();
        projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([artefact]);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(service => service.GetContentAsync(artefact.S3Key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("s3 failed"));

        var controller = new ProjectExportController(
            projectRepository.Object,
            artefactRepository.Object,
            artefactStorageService.Object,
            timeProvider)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.ExportProject(project.Id, CancellationToken.None);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.True(problemDetails.Extensions.ContainsKey("userMessage"));
    }
}