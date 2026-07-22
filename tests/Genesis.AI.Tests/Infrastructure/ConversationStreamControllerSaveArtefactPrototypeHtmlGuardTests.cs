using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Genesis.AI.Api.Features.Conversations;
using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Commands.ProposeRequirementChange;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Configuration;
using Genesis.AI.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Genesis.AI.Tests.Infrastructure;

public class ConversationStreamControllerSaveArtefactPrototypeHtmlGuardTests
{
    [Fact]
    public async Task ExecuteToolCallAsync_SaveArtefactSingleFilePrototypeWithoutDoctype_ReturnsInvalidPrototypeHtml()
    {
        var controller = CreateController(prototypeSingleFileEnabled: true, out _, out _);
        var toolCall = BuildSaveArtefactToolCall("prototype/index.html", "<html><body>partial</body></html>");

        var result = await InvokeExecuteToolCallAsync(controller, toolCall, StageType.Prototype, prototypeSingleFile: true);

        Assert.StartsWith("Error: INVALID_PROTOTYPE_HTML", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteToolCallAsync_SaveArtefactSingleFilePrototypeWithoutHtmlEndTag_ReturnsInvalidPrototypeHtml()
    {
        var controller = CreateController(prototypeSingleFileEnabled: true, out _, out _);
        var toolCall = BuildSaveArtefactToolCall("prototype/index.html", "<!DOCTYPE html><html><body>partial</body>");

        var result = await InvokeExecuteToolCallAsync(controller, toolCall, StageType.Prototype, prototypeSingleFile: true);

        Assert.StartsWith("Error: INVALID_PROTOTYPE_HTML", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteToolCallAsync_SaveArtefactSingleFilePrototypeWithCompleteHtml_IsNotRejectedByInvalidPrototypeHtmlGuard()
    {
        var controller = CreateController(prototypeSingleFileEnabled: true, out var artefactRepositoryMock, out var artefactStorageServiceMock);
        var toolCall = BuildSaveArtefactToolCall(
            "prototype/index.html",
            "<!DOCTYPE html><html><head><title>Prototype</title></head><body><div>PROTOTYPE ONLY</div><h1>OK</h1></body></html>");

        artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);
        artefactRepositoryMock
            .Setup(repository => repository.GetNextVersionForFileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        artefactRepositoryMock
            .SetupGet(repository => repository.UnitOfWork)
            .Returns(unitOfWorkMock.Object);

        artefactRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        artefactRepositoryMock
            .Setup(repository => repository.DeletePreviousVersionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactStorageServiceMock
            .Setup(storageService => storageService.SaveContentAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("projects/test/artefacts/prototype/index.html/v1");

        var result = await InvokeExecuteToolCallAsync(controller, toolCall, StageType.Prototype, prototypeSingleFile: true);

        Assert.DoesNotContain("INVALID_PROTOTYPE_HTML", result, StringComparison.Ordinal);
        Assert.Contains("Saved prototype/index.html (version 1", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteToolCallAsync_SaveArtefactSingleFilePrototypeWithCompleteHtmlAndNoPrototypeOnlyBanner_ReturnsMissingPrototypeBanner()
    {
        var controller = CreateController(prototypeSingleFileEnabled: true, out _, out _);
        var toolCall = BuildSaveArtefactToolCall(
            "prototype/index.html",
            "<!DOCTYPE html><html><head><title>Prototype</title></head><body><h1>OK</h1></body></html>");

        var result = await InvokeExecuteToolCallAsync(controller, toolCall, StageType.Prototype, prototypeSingleFile: true);

        Assert.StartsWith("Error: MISSING_PROTOTYPE_BANNER", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteToolCallAsync_SaveArtefactSingleFilePrototypeWithPrototypeOnlyBanner_IsNotRejectedByPrototypeOnlyGuard()
    {
        var controller = CreateController(prototypeSingleFileEnabled: true, out var artefactRepositoryMock, out var artefactStorageServiceMock);
        var toolCall = BuildSaveArtefactToolCall(
            "prototype/index.html",
            "<!DOCTYPE html><html><head><title>Prototype</title></head><body><div>prototype only</div><h1>OK</h1></body></html>");

        artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);
        artefactRepositoryMock
            .Setup(repository => repository.GetNextVersionForFileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        artefactRepositoryMock
            .SetupGet(repository => repository.UnitOfWork)
            .Returns(unitOfWorkMock.Object);

        artefactRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        artefactRepositoryMock
            .Setup(repository => repository.DeletePreviousVersionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artefactStorageServiceMock
            .Setup(storageService => storageService.SaveContentAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("projects/test/artefacts/prototype/index.html/v1");

        var result = await InvokeExecuteToolCallAsync(controller, toolCall, StageType.Prototype, prototypeSingleFile: true);

        Assert.DoesNotContain("MISSING_PROTOTYPE_BANNER", result, StringComparison.Ordinal);
        Assert.Contains("Saved prototype/index.html (version 1", result, StringComparison.Ordinal);
    }

    private static AiToolCall BuildSaveArtefactToolCall(string filePath, string content)
    {
        var json = JsonSerializer.Serialize(new
        {
            file_path = filePath,
            content
        });

        return new AiToolCall(
            PipelineToolDefinitions.SaveArtefact,
            "tool-use-1",
            JsonDocument.Parse(json));
    }

    private static ConversationStreamController CreateController(
        bool prototypeSingleFileEnabled,
        out Mock<IArtefactRepository> artefactRepositoryMock,
        out Mock<IArtefactStorageService> artefactStorageServiceMock)
    {
        var requirementChangeRepositoryMock = new Mock<IRequirementChangeRepository>();
        requirementChangeRepositoryMock
            .SetupGet(repository => repository.UnitOfWork)
            .Returns(Mock.Of<IUnitOfWork>());

        var proposeRequirementChangeHandler = new ProposeRequirementChangeCommandHandler(requirementChangeRepositoryMock.Object);
        var conversationRepositoryMock = new Mock<IConversationRepository>();
        artefactRepositoryMock = new Mock<IArtefactRepository>();
        artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        var aiServiceMock = new Mock<IAiService>();
        var promptServiceMock = new Mock<IPromptService>();
        var skillContentServiceMock = new Mock<ISkillContentService>();
        var activeSkillsServiceMock = new Mock<IActiveSkillsService>();
        var foundationServiceMock = new Mock<IFoundationService>();
        var sessionCloseContextBuilderMock = new Mock<ISessionCloseContextBuilder>();
        var contractManifestContextBuilderMock = new Mock<IContractManifestContextBuilder>();
        var contractManifestStalenessCheckerMock = new Mock<IContractManifestStalenessChecker>();
        contractManifestStalenessCheckerMock.Setup(checker => checker.CheckStalenessAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        var prototypeAssemblyServiceMock = new Mock<IPrototypeAssemblyService>();
        var prototypeFragmentMigrationServiceMock = new Mock<IPrototypeFragmentMigrationService>();

        var tokenOptions = Options.Create(new TokenOptimisationOptions
        {
            PrototypeSingleFileEnabled = prototypeSingleFileEnabled
        });

        return new ConversationStreamController(
            proposeRequirementChangeHandler,
            conversationRepositoryMock.Object,
            artefactRepositoryMock.Object,
            artefactStorageServiceMock.Object,
            aiServiceMock.Object,
            promptServiceMock.Object,
            skillContentServiceMock.Object,
            activeSkillsServiceMock.Object,
            foundationServiceMock.Object,
            sessionCloseContextBuilderMock.Object,
            contractManifestContextBuilderMock.Object,
            contractManifestStalenessCheckerMock.Object,
            prototypeAssemblyServiceMock.Object,
            prototypeFragmentMigrationServiceMock.Object,
            tokenOptions,
            TimeProvider.System,
            Mock.Of<ILogger<ConversationStreamController>>());
    }

    private static async Task<string> InvokeExecuteToolCallAsync(
        ConversationStreamController controller,
        AiToolCall toolCall,
        StageType stageType,
        bool prototypeSingleFile)
    {
        var method = typeof(ConversationStreamController).GetMethod(
            "ExecuteToolCallAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var contextType = typeof(ConversationStreamController).GetNestedType(
            "ToolExecutionContext",
            BindingFlags.NonPublic | BindingFlags.Public);

        Assert.NotNull(method);
        Assert.NotNull(contextType);

        var context = Activator.CreateInstance(
            contextType!,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new StrongBox<int>(0),
            new StrongBox<bool>(false),
            new StrongBox<bool>(false));

        Assert.NotNull(context);

        var invocation = method!.Invoke(
            controller,
            [
                toolCall,
                new Conversation(Guid.NewGuid(), 6, TimeProvider.System),
                new List<Artefact>(),
                new List<ParkingLotItem>(),
                new List<ParkingLotItem>(),
                "tester",
                Guid.NewGuid(),
                (StageType?)stageType,
                context,
                prototypeSingleFile,
                CancellationToken.None
            ]);

        Assert.NotNull(invocation);
        return await (Task<string>)invocation!;
    }
}
