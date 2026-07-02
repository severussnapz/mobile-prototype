using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.GeneratePrototypeDemo;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Genesis.AI.Tests.PrototypeDemo;
using Moq;
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
            .Setup(service => service.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(PrototypeDemoHtmlAssertions.AsAsyncStream(
                "<!DOCTYPE html><html>", "<body>PROTOTYPE ONLY</body>", "</html>"));

        var handler = new GeneratePrototypeDemoCommandHandler(
            projectRepositoryMock.Object,
            generationServiceMock.Object);

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
            generationServiceMock.Object);

        var result = await handler.Handle(
            new GeneratePrototypeDemoCommand(Guid.NewGuid(), "tester"),
            CancellationToken.None);

        Assert.Equal(GeneratePrototypeDemoStatus.ProjectNotFound, result.Status);
        generationServiceMock.Verify(
            service => service.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
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

    private static async Task<string> GenerateThroughHandlerAsync()
    {
        var project = CreateProject();
        var projectRepositoryMock = new Mock<IProjectRepository>();
        projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var handler = new GeneratePrototypeDemoCommandHandler(
            projectRepositoryMock.Object,
            new StubPrototypeDemoGenerationService());

        var result = await handler.Handle(
            new GeneratePrototypeDemoCommand(project.Id, "tester"),
            CancellationToken.None);

        return result.Html;
    }
}
