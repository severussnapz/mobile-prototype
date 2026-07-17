using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Genesis.AI.Api.Authentication;
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Genesis.AI.Tests.Infrastructure;

public class ConversationStreamControllerContractManifestStalenessWiringTests
{
    [Fact]
    public async Task ExecuteStreamAsync_WhenContractManifestContextNonEmpty_CallsCheckStalenessAsync()
    {
        var controller = CreateController(
            foundationPrefixEnabled: true,
            out var conversationRepositoryMock,
            out _,
            out var aiServiceMock,
            out _,
            out _,
            out var contractManifestContextBuilderMock,
            out var contractManifestStalenessCheckerMock);

        var projectId = Guid.NewGuid();
        var conversation = CreateConversation();
        var request = new StreamMessageRequest
        {
            Content = "Continue design",
            Retry = true
        };
        const string manifestContent = "## Contract Manifest\nManifest version: 3";

        SetupStreamDependencies(
            conversationRepositoryMock,
            aiServiceMock,
            conversation,
            projectId,
            StageType.Design);

        contractManifestContextBuilderMock
            .Setup(builder => builder.BuildContractManifestContextAsync(
                projectId,
                StageType.Design,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifestContent);
        contractManifestStalenessCheckerMock
            .Setup(checker => checker.CheckStalenessAsync(
                projectId,
                manifestContent,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await InvokeExecuteStreamAsync(controller, conversation.Id, request);

        contractManifestStalenessCheckerMock.Verify(
            checker => checker.CheckStalenessAsync(projectId, manifestContent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WhenContractManifestContextEmpty_DoesNotCallCheckStalenessAsync()
    {
        var controller = CreateController(
            foundationPrefixEnabled: true,
            out var conversationRepositoryMock,
            out _,
            out var aiServiceMock,
            out _,
            out _,
            out var contractManifestContextBuilderMock,
            out var contractManifestStalenessCheckerMock);

        var projectId = Guid.NewGuid();
        var conversation = CreateConversation();
        var request = new StreamMessageRequest
        {
            Content = "Continue design",
            Retry = true
        };

        SetupStreamDependencies(
            conversationRepositoryMock,
            aiServiceMock,
            conversation,
            projectId,
            StageType.Design);

        contractManifestContextBuilderMock
            .Setup(builder => builder.BuildContractManifestContextAsync(
                projectId,
                StageType.Design,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        await InvokeExecuteStreamAsync(controller, conversation.Id, request);

        contractManifestStalenessCheckerMock.Verify(
            checker => checker.CheckStalenessAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WhenStalenesWarningsReturned_WarningsAppendedToPrompt()
    {
        var controller = CreateController(
            foundationPrefixEnabled: true,
            out var conversationRepositoryMock,
            out _,
            out var aiServiceMock,
            out _,
            out _,
            out var contractManifestContextBuilderMock,
            out var contractManifestStalenessCheckerMock);

        var projectId = Guid.NewGuid();
        var conversation = CreateConversation();
        var request = new StreamMessageRequest
        {
            Content = "Continue design",
            Retry = true
        };
        const string manifestContent = "## Contract Manifest\nManifest version: 3";
        const string warning = "⚠️ CONTRACT STALE: CONTRACT-MANIFEST.md was produced against requirements/REQ-001.md@v6 but current approved version is v8. Re-run P04 for this requirement before proceeding.";
        AiSystemPrompt? capturedPrompt = null;

        SetupStreamDependencies(
            conversationRepositoryMock,
            aiServiceMock,
            conversation,
            projectId,
            StageType.Design,
            promptCapture: prompt => capturedPrompt = prompt);

        contractManifestContextBuilderMock
            .Setup(builder => builder.BuildContractManifestContextAsync(
                projectId,
                StageType.Design,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifestContent);
        contractManifestStalenessCheckerMock
            .Setup(checker => checker.CheckStalenessAsync(
                projectId,
                manifestContent,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([warning]);

        await InvokeExecuteStreamAsync(controller, conversation.Id, request);

        Assert.NotNull(capturedPrompt);
        Assert.Contains(warning, capturedPrompt!.MutablePart, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WhenNoStalenessWarnings_NothingExtraAppendedToPrompt()
    {
        var controller = CreateController(
            foundationPrefixEnabled: true,
            out var conversationRepositoryMock,
            out _,
            out var aiServiceMock,
            out _,
            out _,
            out var contractManifestContextBuilderMock,
            out var contractManifestStalenessCheckerMock);

        var projectId = Guid.NewGuid();
        var conversation = CreateConversation();
        var request = new StreamMessageRequest
        {
            Content = "Continue design",
            Retry = true
        };
        const string manifestContent = "## Contract Manifest\nManifest version: 3";
        AiSystemPrompt? capturedPrompt = null;

        SetupStreamDependencies(
            conversationRepositoryMock,
            aiServiceMock,
            conversation,
            projectId,
            StageType.Design,
            promptCapture: prompt => capturedPrompt = prompt);

        contractManifestContextBuilderMock
            .Setup(builder => builder.BuildContractManifestContextAsync(
                projectId,
                StageType.Design,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifestContent);
        contractManifestStalenessCheckerMock
            .Setup(checker => checker.CheckStalenessAsync(
                projectId,
                manifestContent,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await InvokeExecuteStreamAsync(controller, conversation.Id, request);

        Assert.NotNull(capturedPrompt);
        Assert.DoesNotContain("CONTRACT STALE", capturedPrompt!.MutablePart, StringComparison.Ordinal);
    }

    private static Conversation CreateConversation()
    {
        return new Conversation(Guid.NewGuid(), 6, TimeProvider.System);
    }

    private static ConversationStreamController CreateController(
        bool foundationPrefixEnabled,
        out Mock<IConversationRepository> conversationRepositoryMock,
        out Mock<IArtefactRepository> artefactRepositoryMock,
        out Mock<IAiService> aiServiceMock,
        out Mock<IPromptService> promptServiceMock,
        out Mock<IFoundationService> foundationServiceMock,
        out Mock<IContractManifestContextBuilder> contractManifestContextBuilderMock,
        out Mock<IContractManifestStalenessChecker> contractManifestStalenessCheckerMock)
    {
        var requirementChangeRepositoryMock = new Mock<IRequirementChangeRepository>();
        requirementChangeRepositoryMock
            .SetupGet(repository => repository.UnitOfWork)
            .Returns(Mock.Of<IUnitOfWork>());

        var proposeRequirementChangeHandler = new ProposeRequirementChangeCommandHandler(requirementChangeRepositoryMock.Object);
        conversationRepositoryMock = new Mock<IConversationRepository>();
        artefactRepositoryMock = new Mock<IArtefactRepository>();
        aiServiceMock = new Mock<IAiService>();
        promptServiceMock = new Mock<IPromptService>();
        foundationServiceMock = new Mock<IFoundationService>();
        contractManifestContextBuilderMock = new Mock<IContractManifestContextBuilder>();
        contractManifestStalenessCheckerMock = new Mock<IContractManifestStalenessChecker>();
        var artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        var skillContentServiceMock = new Mock<ISkillContentService>();
        var activeSkillsServiceMock = new Mock<IActiveSkillsService>();
        var sessionCloseContextBuilderMock = new Mock<ISessionCloseContextBuilder>();
        var prototypeAssemblyServiceMock = new Mock<IPrototypeAssemblyService>();
        var prototypeFragmentMigrationServiceMock = new Mock<IPrototypeFragmentMigrationService>();

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        conversationRepositoryMock
            .SetupGet(repository => repository.UnitOfWork)
            .Returns(unitOfWorkMock.Object);

        promptServiceMock
            .Setup(service => service.GetSystemPrompt(It.IsAny<StageType>()))
            .Returns("Base prompt");
        foundationServiceMock
            .Setup(service => service.BuildFoundationContentAsync(
                It.IsAny<Guid>(),
                It.IsAny<StageType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        sessionCloseContextBuilderMock
            .Setup(builder => builder.BuildSessionCloseContextAsync(
                It.IsAny<Guid>(),
                It.IsAny<StageType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var tokenOptions = Options.Create(new TokenOptimisationOptions
        {
            FoundationPrefixEnabled = foundationPrefixEnabled
        });

        var controller = new ConversationStreamController(
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

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("authorizations", AuthorisationScopes.Write)
                ],
                "test"))
            }
        };

        return controller;
    }

    private static void SetupStreamDependencies(
        Mock<IConversationRepository> conversationRepositoryMock,
        Mock<IAiService> aiServiceMock,
        Conversation conversation,
        Guid projectId,
        StageType? stageType,
        Action<AiSystemPrompt>? promptCapture = null)
    {
        conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithMessagesAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        conversationRepositoryMock
            .Setup(repository => repository.GetStageTypeByStageIdAsync(conversation.StageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stageType);
        conversationRepositoryMock
            .Setup(repository => repository.GetProjectContextByStageIdAsync(conversation.StageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectContext(projectId, "TEST", "Test Project", "Description", ComplianceDomain.Generic));
        conversationRepositoryMock
            .Setup(repository => repository.GetParkingLotByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        aiServiceMock
            .Setup(service => service.StreamWithToolsAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<IReadOnlyList<AiToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .Callback<AiSystemPrompt, IReadOnlyList<AiMessage>, IReadOnlyList<AiToolDefinition>, CancellationToken>(
                (systemPrompt, _, _, _) => promptCapture?.Invoke(systemPrompt))
            .Returns(CreateStreamEvents(
            [
                new AiTextChunk("Understood.")
            ]));
    }

    private static async Task InvokeExecuteStreamAsync(
        ConversationStreamController controller,
        Guid conversationId,
        StreamMessageRequest request)
    {
        var method = typeof(ConversationStreamController).GetMethod(
            "ExecuteStreamAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var invocation = method!.Invoke(
            controller,
            [conversationId, request, CancellationToken.None]);

        Assert.NotNull(invocation);
        await (Task)invocation!;
    }

    private static async IAsyncEnumerable<AiStreamEvent> CreateStreamEvents(
        IReadOnlyList<AiStreamEvent> events,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var streamEvent in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return streamEvent;
            await Task.Yield();
        }
    }
}