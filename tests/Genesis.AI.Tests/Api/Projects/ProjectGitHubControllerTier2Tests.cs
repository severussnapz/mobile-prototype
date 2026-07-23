using System.Reflection;
using System.Security.Claims;
using Genesis.AI.Api.Features.Projects;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetArtefactsByStage;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Tests.Api.Projects;

public class ProjectGitHubControllerTier2Tests
{
    [Fact]
    public async Task PushAll_WhenBackgroundPushStarts_ReturnsAccepted()
    {
        var scenario = CreatePushAllFailureScenario();

        var result = await scenario.Controller.PushAll(scenario.ProjectId, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(StatusCodes.Status202Accepted, accepted.StatusCode);
    }

    [Fact]
    public async Task PushAll_WhenBackgroundPushFails_LogsFailureToPushFailureLog()
    {
        var scenario = CreatePushAllFailureScenario();

        await scenario.Controller.PushAll(scenario.ProjectId, CancellationToken.None);

        await scenario.PushAttempted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        scenario.PushFailureLogRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PushFailureLog>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task PushAllBestEffortAsync_WhenPerArtefactPushFails_PersistsToFailureLog()
    {
        var projectId = Guid.NewGuid();
        var artefact = CreateArtefact(projectId, Guid.NewGuid());

        var mediator = new Mock<IMediator>();
        mediator
            .Setup(mock => mock.Send(It.IsAny<GetArtefactsByStageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([artefact]);

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
            .ThrowsAsync(new InvalidOperationException("push failed"));

        var pushFailureLogRepository = new Mock<IPushFailureLogRepository>();
        pushFailureLogRepository
            .Setup(repository => repository.AddAsync(It.IsAny<PushFailureLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(mediator.Object, pushService.Object, pushFailureLogRepository.Object);
        var method = typeof(ProjectGitHubController).GetMethod("PushAllBestEffortAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var invocation = method!.Invoke(controller, [projectId, "tester@example.com", CreateScopeFactory(mediator.Object, pushService.Object, pushFailureLogRepository.Object)]);

        Assert.NotNull(invocation);
        await (Task)invocation!;

        pushFailureLogRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PushFailureLog>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    private static ProjectGitHubController CreateController(
        IMediator mediator,
        IGitHubArtefactPushService pushService,
        IPushFailureLogRepository pushFailureLogRepository)
    {
        var controller = new ProjectGitHubController(
            mediator,
            Mock.Of<ILogger<ProjectGitHubController>>(),
            pushService,
            scaffolder: null,
            serviceScopeFactory: CreateScopeFactory(mediator, pushService, pushFailureLogRepository));

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

    private static PushAllFailureScenario CreatePushAllFailureScenario()
    {
        var projectId = Guid.NewGuid();
        var artefact = CreateArtefact(projectId, Guid.NewGuid());
        var pushAttempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var mediator = new Mock<IMediator>();
        mediator
            .Setup(mock => mock.Send(It.IsAny<GetArtefactsByStageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([artefact]);

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
            .Returns(() =>
            {
                pushAttempted.TrySetResult();
                throw new InvalidOperationException("push failed");
            });

        var pushFailureLogRepository = new Mock<IPushFailureLogRepository>();
        pushFailureLogRepository
            .Setup(repository => repository.AddAsync(It.IsAny<PushFailureLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new PushAllFailureScenario(
            projectId,
            CreateController(mediator.Object, pushService.Object, pushFailureLogRepository.Object),
            pushFailureLogRepository,
            pushAttempted);
    }

    private static IServiceScopeFactory CreateScopeFactory(
        IMediator mediator,
        IGitHubArtefactPushService pushService,
        IPushFailureLogRepository pushFailureLogRepository)
    {
        var services = new ServiceCollection();
        services.AddSingleton(mediator);
        services.AddSingleton(pushService);
        services.AddSingleton(pushFailureLogRepository);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Mock.Of<ILogger<ProjectGitHubController>>());

        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static Artefact CreateArtefact(Guid projectId, Guid artefactId)
    {
        var artefact = Artefact.CreateS3Artefact(
            projectId,
            version: 1,
            filePath: "requirements/REQ-001.md",
            s3Key: "projects/test/artefacts/requirements/REQ-001.md/v1",
            contentType: "text/markdown",
            sizeBytes: 42,
            createdBy: "tester@example.com",
            timeProvider: TimeProvider.System,
            isPublished: true);

        typeof(Artefact).GetProperty("Id")!.SetValue(artefact, artefactId);
        return artefact;
    }

    private sealed record PushAllFailureScenario(
        Guid ProjectId,
        ProjectGitHubController Controller,
        Mock<IPushFailureLogRepository> PushFailureLogRepository,
        TaskCompletionSource PushAttempted);
}