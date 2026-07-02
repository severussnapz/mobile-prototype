using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.GeneratePrototypeDemo;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Genesis.AI.Tests.PrototypeDemo;
using Moq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Genesis.AI.Tests.Commands;

// Day 0 harness (handler level): pins the handler orchestration contract AND
// re-runs the four content checks end-to-end through the handler with the real
// stub service, proving the handler concatenates the IAsyncEnumerable stream
// without corrupting the HTML. Fails to compile until the command slice
// (GeneratePrototypeDemoCommand / Handler / Result / Status) and
// IPrototypeDemoGenerationService exist. Non-compiling state IS the red.
public class GeneratePrototypeDemoCommandTests
{
    private static readonly IPrototypeDemoSettings DefaultPrototypeDemoSettings =
        new FixedPrototypeDemoSettings(TimeSpan.FromMinutes(3));

    private static Project CreateProject()
    {
        return new Project(
            code: "DEMO",
            name: "Demo Project",
            description: null,
            timeSheetCode: "TS-001",
            complianceDomain: ComplianceDomain.Generic,
            createdBy: "tester",
            timeProvider: TimeProvider.System);
    }

    // --- orchestration (mocked streaming service) ---

    [Fact]
    public async Task Handle_WhenProjectExists_ReturnsSuccessWithConcatenatedHtml()
    {
        var project = CreateProject();
        var projectRepositoryMock = new Mock<IProjectRepository>();
        projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var generationServiceMock = new Mock<IPrototypeDemoGenerationService>();
        generationServiceMock
            .Setup(service => service.GenerateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(PrototypeDemoHtmlAssertions.AsAsyncStream(
                "<!DOCTYPE html><html>", "<body>PROTOTYPE ONLY</body>", "</html>"));

        var handler = new GeneratePrototypeDemoCommandHandler(
            projectRepositoryMock.Object,
            generationServiceMock.Object,
            DefaultPrototypeDemoSettings);

        var result = await handler.Handle(
            new GeneratePrototypeDemoCommand(project.Id, "tester"),
            CancellationToken.None);

        Assert.Equal(GeneratePrototypeDemoStatus.Success, result.Status);
        Assert.Equal("<!DOCTYPE html><html><body>PROTOTYPE ONLY</body></html>", result.Html);
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ReturnsProjectNotFoundStatus()
    {
        var projectRepositoryMock = new Mock<IProjectRepository>();
        projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var generationServiceMock = new Mock<IPrototypeDemoGenerationService>();

        var handler = new GeneratePrototypeDemoCommandHandler(
            projectRepositoryMock.Object,
            generationServiceMock.Object,
            DefaultPrototypeDemoSettings);

        var result = await handler.Handle(
            new GeneratePrototypeDemoCommand(Guid.NewGuid(), "tester"),
            CancellationToken.None);

        Assert.Equal(GeneratePrototypeDemoStatus.ProjectNotFound, result.Status);
        generationServiceMock.Verify(
            service => service.GenerateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // --- content checks through the handler with the real stub service ---

    [Fact]
    public async Task Handle_WithRealStubService_IncludesPrototypeOnlyBanner()
    {
        PrototypeDemoHtmlAssertions.AssertContainsPrototypeOnlyBanner(await GenerateThroughHandlerAsync());
    }

    [Fact]
    public async Task Handle_WithRealStubService_InlinesEmisXBaseCssIntoHead()
    {
        PrototypeDemoHtmlAssertions.AssertEmisXBaseCssInlinedIntoHead(await GenerateThroughHandlerAsync());
    }

    [Fact]
    public async Task Handle_WithRealStubService_ReturnsCompleteHtmlDocument()
    {
        PrototypeDemoHtmlAssertions.AssertCompleteHtmlDocument(await GenerateThroughHandlerAsync());
    }

    [Fact]
    public async Task Handle_WithRealStubService_ContainsNoFormatValidNhsNumbers()
    {
        PrototypeDemoHtmlAssertions.AssertNoFormatValidNhsNumbers(await GenerateThroughHandlerAsync());
    }

    [Fact]
    public async Task Handle_WhenConfiguredTimeoutElapses_ReturnsTimedOutStatus()
    {
        var project = CreateProject();
        var projectRepositoryMock = new Mock<IProjectRepository>();
        projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var generationServiceMock = new Mock<IPrototypeDemoGenerationService>();
        generationServiceMock
            .Setup(service => service.GenerateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, string _, CancellationToken cancellationToken) => NeverCompletes(cancellationToken));

        var handler = new GeneratePrototypeDemoCommandHandler(
            projectRepositoryMock.Object,
            generationServiceMock.Object,
            new FixedPrototypeDemoSettings(TimeSpan.Zero));

        var result = await handler.Handle(
            new GeneratePrototypeDemoCommand(project.Id, "tester"),
            CancellationToken.None);

        Assert.Equal(GeneratePrototypeDemoStatus.TimedOut, result.Status);
    }

    private static async Task<string> GenerateThroughHandlerAsync()
    {
        var project = CreateProject();
        var projectRepositoryMock = new Mock<IProjectRepository>();
        projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var handler = new GeneratePrototypeDemoCommandHandler(
            projectRepositoryMock.Object,
            new StubPrototypeDemoGenerationService(),
            DefaultPrototypeDemoSettings);

        var result = await handler.Handle(
            new GeneratePrototypeDemoCommand(project.Id, "tester"),
            CancellationToken.None);

        return result.Html;
    }

    private static async IAsyncEnumerable<string> NeverCompletes([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        yield break;
    }

    private sealed class FixedPrototypeDemoSettings : IPrototypeDemoSettings
    {
        public FixedPrototypeDemoSettings(TimeSpan generationTimeout)
        {
            GenerationTimeout = generationTimeout;
        }

        public TimeSpan GenerationTimeout { get; }
    }
}
