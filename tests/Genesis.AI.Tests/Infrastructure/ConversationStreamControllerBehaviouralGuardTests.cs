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

public class ConversationStreamControllerBehaviouralGuardTests
{
    [Fact]
    public async Task ExecuteToolCallAsync_WhenZeroMatchToolBlocked_BlocksApplyToScope()
    {
        var controller = CreateController(out _, out _);
        var toolCall = BuildToolCall(PipelineToolDefinitions.ApplyToScope, new
        {
            scope = "screen-01",
            selector = ".chip",
            operation = "set_text",
            strategy = "literal",
            value = "Updated"
        });

        var result = await InvokeExecuteToolCallAsync(
            controller,
            toolCall,
            new StrongBox<bool>(false),
            new StrongBox<bool>(true),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.Contains("HARD STOP ALREADY TRIGGERED", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteToolCallAsync_WhenZeroMatchToolBlocked_AllowsOtherTools()
    {
        var controller = CreateController(out _, out _);
        var toolCall = BuildToolCall(PipelineToolDefinitions.AddParkingLotItem, new
        {
            priority = "High",
            content = "Needs user clarification"
        });

        var result = await InvokeExecuteToolCallAsync(
            controller,
            toolCall,
            new StrongBox<bool>(false),
            new StrongBox<bool>(true),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.DoesNotContain("HARD STOP ALREADY TRIGGERED", result, StringComparison.Ordinal);
        Assert.Equal("Item added to parking lot", result);
    }

    [Fact]
    public async Task ExecuteToolCallAsync_WhenPostSearchReadBlocked_BlocksFragmentRead()
    {
        var controller = CreateController(out _, out _);
        var toolCall = BuildToolCall(PipelineToolDefinitions.GetArtefact, new
        {
            file_path = "prototype/fragments/screen-01.html"
        });

        var result = await InvokeExecuteToolCallAsync(
            controller,
            toolCall,
            new StrongBox<bool>(true),
            new StrongBox<bool>(false),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.Contains("BLOCKED", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteToolCallAsync_WhenPostSearchReadBlocked_BlocksRequirementRead()
    {
        var controller = CreateController(out _, out _);
        var toolCall = BuildToolCall(PipelineToolDefinitions.GetArtefact, new
        {
            file_path = "requirements/REQ-001.md"
        });

        var result = await InvokeExecuteToolCallAsync(
            controller,
            toolCall,
            new StrongBox<bool>(true),
            new StrongBox<bool>(false),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.Contains("BLOCKED", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteToolCallAsync_WhenPostSearchReadBlocked_AllowsNonFragmentRead()
    {
        var controller = CreateController(out _, out _);
        var toolCall = BuildToolCall(PipelineToolDefinitions.GetArtefact, new
        {
            file_path = "architecture/ARCH.md"
        });

        var result = await InvokeExecuteToolCallAsync(
            controller,
            toolCall,
            new StrongBox<bool>(true),
            new StrongBox<bool>(false),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.DoesNotContain("BLOCKED", result, StringComparison.Ordinal);
        Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteToolCallAsync_WhenFileReadInSameTurn_BlocksEditArtefact()
    {
        var controller = CreateController(out _, out _);
        var toolCall = BuildToolCall(PipelineToolDefinitions.EditArtefact, new
        {
            file_path = "prototype/fragments/screen-01.html",
            old_str = "before",
            new_str = "after"
        });

        var filesReadThisRequest = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "prototype/fragments/screen-01.html"
        };
        var filesReadThisTurn = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "prototype/fragments/screen-01.html"
        };

        var result = await InvokeExecuteToolCallAsync(
            controller,
            toolCall,
            new StrongBox<bool>(false),
            new StrongBox<bool>(false),
            filesReadThisRequest,
            filesReadThisTurn);

        Assert.Contains("FILE_READ_SAME_TURN", result, StringComparison.Ordinal);
    }

    private static AiToolCall BuildToolCall(string toolName, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return new AiToolCall(toolName, "tool-use-1", JsonDocument.Parse(json));
    }

    private static ConversationStreamController CreateController(
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

        artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var tokenOptions = Options.Create(new TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true,
            PrototypeSingleFileEnabled = false
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
        StrongBox<bool> postSearchReadBlocked,
        StrongBox<bool> zeroMatchToolBlocked,
        HashSet<string> filesReadThisRequest,
        HashSet<string> filesReadThisTurn)
    {
        var method = typeof(ConversationStreamController).GetMethod(
            "ExecuteToolCallAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

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
                (StageType?)StageType.Prototype,
                filesReadThisRequest,
                filesReadThisTurn,
                new StrongBox<int>(0),
                postSearchReadBlocked,
                zeroMatchToolBlocked,
                false,
                CancellationToken.None
            ]);

        Assert.NotNull(invocation);
        return await (Task<string>)invocation!;
    }
}