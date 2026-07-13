using System.Security.Claims;
using AutoMapper;
using Genesis.AI.Api.Features.Projects;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetArtefactById;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Tests.Api.Projects;

public class PushToGitHubTests
{
    [Fact]
    public async Task PushAll_ReturnsAccepted()
    {
        var projectId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        var pushService = new Mock<IGitHubArtefactPushService>();
        var controller = CreateController(mediator, pushService);

        var result = await controller.PushAll(projectId, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(StatusCodes.Status202Accepted, accepted.StatusCode);
    }

    [Fact]
    public async Task PushAll_NoGitHubConfig_StillReturnsAccepted()
    {
        var projectId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(mock => mock.Send(It.IsAny<GetArtefactByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var pushService = new Mock<IGitHubArtefactPushService>();
        pushService
            .Setup(mock => mock.PushAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("missing github config"));

        var controller = CreateController(mediator, pushService);

        var result = await controller.PushAll(projectId, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(StatusCodes.Status202Accepted, accepted.StatusCode);
    }

    [Fact]
    public async Task PushArtefact_Success_Returns200WithUserMessage()
    {
        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();

        var artefact = Artefact.CreateS3Artefact(
            projectId,
            version: 2,
            filePath: "requirements/REQ-002-electronic-inbound-ingestion.md",
            s3Key: "projects/p1/artefacts/requirements/REQ-002-electronic-inbound-ingestion.md/v2",
            contentType: "text/markdown",
            sizeBytes: 42,
            createdBy: "tester@example.com",
            timeProvider: TimeProvider.System,
            isPublished: true);

        typeof(Artefact).GetProperty("Id")!.SetValue(artefact, artefactId);

        var mediator = new Mock<IMediator>();
        mediator
            .Setup(mock => mock.Send(It.IsAny<GetArtefactByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(artefact);

        var pushService = new Mock<IGitHubArtefactPushService>();
        var controller = CreateController(mediator, pushService);

        var result = await controller.PushArtefact(projectId, artefactId, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var message = ExtractUserMessage(created.Value);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public async Task PushArtefact_ArtefactNotFound_Returns404()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(mock => mock.Send(It.IsAny<GetArtefactByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var pushService = new Mock<IGitHubArtefactPushService>();
        var controller = CreateController(mediator, pushService);

        var result = await controller.PushArtefact(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task PushArtefact_GitHubAuthFails_Returns503WithUserMessage()
    {
        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();

        var artefact = Artefact.CreateS3Artefact(
            projectId,
            version: 2,
            filePath: "requirements/REQ-002-electronic-inbound-ingestion.md",
            s3Key: "projects/p1/artefacts/requirements/REQ-002-electronic-inbound-ingestion.md/v2",
            contentType: "text/markdown",
            sizeBytes: 42,
            createdBy: "tester@example.com",
            timeProvider: TimeProvider.System,
            isPublished: true);

        typeof(Artefact).GetProperty("Id")!.SetValue(artefact, artefactId);

        var mediator = new Mock<IMediator>();
        mediator
            .Setup(mock => mock.Send(It.IsAny<GetArtefactByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(artefact);

        var pushService = new Mock<IGitHubArtefactPushService>();
        pushService
            .Setup(mock => mock.PushAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitHubAuthenticationException("auth failed"));

        var controller = CreateController(mediator, pushService);

        var result = await controller.PushArtefact(projectId, artefactId, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
        var message = ExtractUserMessage(status.Value);
        Assert.Contains("GitHub", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("connect", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PushArtefact_FileTooLarge_Returns503WithUserMessage()
    {
        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();

        var artefact = Artefact.CreateS3Artefact(
            projectId,
            version: 2,
            filePath: "requirements/REQ-002-electronic-inbound-ingestion.md",
            s3Key: "projects/p1/artefacts/requirements/REQ-002-electronic-inbound-ingestion.md/v2",
            contentType: "text/markdown",
            sizeBytes: 42,
            createdBy: "tester@example.com",
            timeProvider: TimeProvider.System,
            isPublished: true);

        typeof(Artefact).GetProperty("Id")!.SetValue(artefact, artefactId);

        var mediator = new Mock<IMediator>();
        mediator
            .Setup(mock => mock.Send(It.IsAny<GetArtefactByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(artefact);

        var pushService = new Mock<IGitHubArtefactPushService>();
        pushService
            .Setup(mock => mock.PushAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitHubFileTooLargeException("too large"));

        var controller = CreateController(mediator, pushService);

        var result = await controller.PushArtefact(projectId, artefactId, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
        var message = ExtractUserMessage(status.Value);
        Assert.Contains("too large", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("12", message, StringComparison.OrdinalIgnoreCase);
    }

    private static ProjectGitHubController CreateController(
        Mock<IMediator> mediator,
        Mock<IGitHubArtefactPushService> pushService)
    {
        var logger = new Mock<ILogger<ProjectGitHubController>>();
        var scaffolder = new Mock<IGenesisStructureScaffolder>();

        var controllerType = typeof(ProjectGitHubController);
        var constructors = controllerType.GetConstructors();

        object? instance = null;
        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var args = new object?[parameters.Length];
            var compatible = true;

            for (var index = 0; index < parameters.Length; index++)
            {
                var parameterType = parameters[index].ParameterType;

                if (parameterType == typeof(IMediator))
                {
                    args[index] = mediator.Object;
                    continue;
                }

                if (parameterType == typeof(ILogger<ProjectGitHubController>))
                {
                    args[index] = logger.Object;
                    continue;
                }

                if (parameterType == typeof(IGitHubArtefactPushService))
                {
                    args[index] = pushService.Object;
                    continue;
                }

                if (parameterType == typeof(IGenesisStructureScaffolder))
                {
                    args[index] = scaffolder.Object;
                    continue;
                }

                if (parameterType == typeof(IServiceScopeFactory))
                {
                    args[index] = null;  // Not used in tests; passed null for optional parameter
                    continue;
                }

                compatible = false;
                break;
            }

            if (!compatible)
            {
                continue;
            }

            instance = constructor.Invoke(args);
            break;
        }

        if (instance is not ProjectGitHubController controller)
        {
            throw new InvalidOperationException("Unable to construct ProjectGitHubController for tests.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "tester@example.com"),
            new Claim("sub", "tester-sub"),
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")),
            },
        };

        return controller;
    }

    private static string ExtractUserMessage(object? responseBody)
        => responseBody is PushActionResponse response ? response.UserMessage : string.Empty;
}
