using Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate;
using Genesis.AI.Infrastructure;
using Genesis.AI.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Genesis.AI.Tests.Infrastructure.Repositories;

public sealed class PushFailureLogRepositoryTests
{
    [Fact]
    public async Task GetFailedArtefactIdsAsync_NoFailures_ReturnsEmpty()
    {
        await using var context = CreateContext();
        var repository = new PushFailureLogRepository(context);

        var result = await repository.GetFailedArtefactIdsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFailedArtefactIdsAsync_OneFailure_ReturnsArtefactId()
    {
        await using var context = CreateContext();
        var repository = new PushFailureLogRepository(context);

        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();

        context.PushFailureLogs.Add(new PushFailureLog(
            projectId,
            artefactId,
            "requirements/REQ-001.md",
            "push failed",
            TimeProvider.System));
        await context.SaveChangesAsync();

        var result = await repository.GetFailedArtefactIdsAsync(projectId, CancellationToken.None);

        Assert.Contains(artefactId, result);
    }

    [Fact]
    public async Task GetFailedArtefactIdsAsync_ResolvedFailure_NotReturned()
    {
        await using var context = CreateContext();
        var repository = new PushFailureLogRepository(context);

        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();

        var resolved = new PushFailureLog(
            projectId,
            artefactId,
            "requirements/REQ-001.md",
            "push failed",
            TimeProvider.System);

        typeof(PushFailureLog)
            .GetProperty(nameof(PushFailureLog.ResolvedAt))!
            .SetValue(resolved, DateTimeOffset.UtcNow);

        context.PushFailureLogs.Add(resolved);
        await context.SaveChangesAsync();

        var result = await repository.GetFailedArtefactIdsAsync(projectId, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFailedArtefactIdsAsync_DuplicateFailures_ReturnsDistinct()
    {
        await using var context = CreateContext();
        var repository = new PushFailureLogRepository(context);

        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();

        context.PushFailureLogs.Add(new PushFailureLog(
            projectId,
            artefactId,
            "requirements/REQ-001.md",
            "push failed 1",
            TimeProvider.System));

        context.PushFailureLogs.Add(new PushFailureLog(
            projectId,
            artefactId,
            "requirements/REQ-001.md",
            "push failed 2",
            TimeProvider.System));

        await context.SaveChangesAsync();

        var result = await repository.GetFailedArtefactIdsAsync(projectId, CancellationToken.None);

        Assert.Single(result, artefactId);
    }

    [Fact]
    public async Task GetFailedArtefactIdsAsync_DifferentProject_NotReturned()
    {
        await using var context = CreateContext();
        var repository = new PushFailureLogRepository(context);

        var targetProjectId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();

        context.PushFailureLogs.Add(new PushFailureLog(
            otherProjectId,
            Guid.NewGuid(),
            "requirements/REQ-001.md",
            "push failed",
            TimeProvider.System));

        await context.SaveChangesAsync();

        var result = await repository.GetFailedArtefactIdsAsync(targetProjectId, CancellationToken.None);

        Assert.Empty(result);
    }

    private static GenesisAiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GenesisAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var mediator = new Mock<IMediator>().Object;
        return new GenesisAiDbContext(options, mediator);
    }
}
