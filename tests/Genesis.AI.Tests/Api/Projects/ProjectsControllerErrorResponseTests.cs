using System.Security.Claims;
using AutoMapper;
using Genesis.AI.Api.Features.Projects;
using Genesis.AI.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Tests.Api.Projects;

public class ProjectsControllerErrorResponseTests
{
    [Fact]
    public async Task UpdateDetails_WhenProjectNotFound_ReturnsUserMessage()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(mock => mock.Send(It.IsAny<Genesis.AI.Domain.Commands.UpdateProjectDetails.UpdateProjectDetailsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("missing project"));

        var controller = CreateController(mediator.Object);

        var result = await controller.UpdateDetails(
            Guid.NewGuid(),
            new UpdateProjectDetailsRequest { Name = "Name", TimeSheetCode = "PORTASK0001045", ComplianceDomain = "Generic" },
            CancellationToken.None);

        AssertHasUserMessage(result, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task UpdateGitHub_WhenServiceUnavailable_ReturnsUserMessage()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(mock => mock.Send(It.IsAny<Genesis.AI.Domain.Commands.UpdateProjectGitHub.UpdateProjectGitHubCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var controller = CreateController(mediator.Object);

        var result = await controller.UpdateGitHub(
            Guid.NewGuid(),
            new UpdateProjectGitHubRequest
            {
                GitHubApiRepoUrl = "https://github.com/org/api",
                GitHubAppRepoUrl = "https://github.com/org/app",
                FigmaFileUrl = "https://figma.com/file/abc"
            },
            CancellationToken.None);

        AssertHasUserMessage(result, StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task UpdateP00_WhenProjectNotFound_ReturnsUserMessage()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(mock => mock.Send(It.IsAny<Genesis.AI.Domain.Commands.UpdateProjectP00.UpdateProjectP00Command>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("missing project"));

        var controller = CreateController(mediator.Object);

        var result = await controller.UpdateP00(
            Guid.NewGuid(),
            new UpdateProjectP00Request { ReleaseType = "Minor", AssuranceRequired = true },
            CancellationToken.None);

        AssertHasUserMessage(result, StatusCodes.Status404NotFound);
    }

    private static ProjectsController CreateController(IMediator mediator)
    {
        var controller = new ProjectsController(
            mediator,
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<ProjectsController>>());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Email, "tester@example.com"),
                    new Claim("sub", "tester-sub")
                ], "TestAuth"))
            }
        };

        return controller;
    }

    private static void AssertHasUserMessage(IActionResult result, int expectedStatusCode)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.True(problemDetails.Extensions.ContainsKey("userMessage"));
    }
}