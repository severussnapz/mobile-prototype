using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Genesis.AI.Tests.PrototypeDemo;

namespace Genesis.AI.Tests.Infrastructure;

// Day 2 failing tests for the real Bedrock-backed prototype-demo generator.
// These reference BedrockPrototypeDemoGenerationService, which does NOT yet
// exist — that missing type is the intended TDD red (same pattern as Day 0/Day 1,
// a compile-time red on the missing symbol). A mocked IAiService keeps the tests
// deterministic and free of network/token cost.
//
// Two layers of assertion:
//  1. Service-contribution tests — what the service itself adds/does: inlines
//     emis-x-base.css into <head>, composes requirements + UI kit into the system
//     prompt, and loads requirements by projectId.
//  2. Contract-preservation tests — feed a spec-compliant golden model output
//     through the service and assert the final artefact still satisfies all six
//     Day 0 harness checks. With a mocked model these prove the service's CSS
//     injection does not BREAK any contract property (the model behaviour itself
//     is validated by the live prompt harness, not by a mock).
public sealed class BedrockPrototypeDemoGenerationServiceTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string ProjectName = "Allergy Recorder";
    private const string RequirementMarker = "REQ-001: Clinician can record a patient allergy.";
    private const string RequirementS3Key = "s3-req-001";

    // A spec-compliant exemplar of what the draft v0.1 prompt instructs the model
    // to emit: complete document, exact banner, its own component <style> (NOT the
    // base stylesheet — the prompt says that is already injected), fictional data
    // with the sanctioned obvious-fake NHS number, five data-screen sections plus a
    // deferred-screens comment, and no external CDN references (emis-x mode).
    private const string GoldenModelHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <title>Allergy Recorder — Prototype</title>
          <style>.screen{padding:1rem}</style>
        </head>
        <body>
          <div role="banner">PROTOTYPE ONLY — Requirements validation artefact. Not for production use.</div>
          <section data-screen="list"><h1>Allergies</h1><p>Alex Sample — NHS 000 000 0000</p></section>
          <section data-screen="add"><h2>Add allergy</h2></section>
          <section data-screen="detail"><h2>Allergy detail</h2></section>
          <section data-screen="edit"><h2>Edit allergy</h2></section>
          <section data-screen="confirm"><h2>Confirm</h2></section>
          <!-- Deferred screens (beyond the 5 most important): audit-history, print-view -->
          <script>console.log('prototype');</script>
        </body>
        </html>
        """;

    private sealed record Harness(
        BedrockPrototypeDemoGenerationService Service,
        Mock<IAiService> Ai,
        Mock<IArtefactRepository> Artefacts,
        Mock<IArtefactStorageService> Storage);

    private static Harness CreateHarness(string modelHtml = GoldenModelHtml)
    {
        var ai = new Mock<IAiService>();
        ai.Setup(service => service.StreamResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
          .Returns(PrototypeDemoHtmlAssertions.AsAsyncStream(modelHtml));

        var requirement = Artefact.CreateS3Artefact(
            ProjectId, 1, "requirements/REQ-001.md", RequirementS3Key,
            "text/markdown", 42, "tester", TimeProvider.System, isPublished: true);

        var artefacts = new Mock<IArtefactRepository>();
        artefacts.Setup(repo => repo.GetByProjectIdAsync(ProjectId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[] { requirement });

        var storage = new Mock<IArtefactStorageService>();
        storage.Setup(store => store.GetContentAsync(RequirementS3Key, It.IsAny<CancellationToken>()))
               .ReturnsAsync(RequirementMarker);

        var service = new BedrockPrototypeDemoGenerationService(ai.Object, artefacts.Object, storage.Object, new PrototypeDocumentAssembler());
        return new Harness(service, ai, artefacts, storage);
    }

    private static Task<string> GenerateAsync(Harness harness)
    {
        return PrototypeDemoHtmlAssertions.CollectAsync(
            harness.Service.GenerateAsync(ProjectId, ProjectName, CancellationToken.None));
    }

    // ---- Service-contribution tests ----

    [Fact]
    public async Task GenerateAsync_WhenCalled_InlinesEmisXBaseCssIntoHead()
    {
        var harness = CreateHarness();

        var html = await GenerateAsync(harness);

        PrototypeDemoHtmlAssertions.AssertEmisXBaseCssInlinedIntoHead(html);
    }

    [Fact]
    public async Task GenerateAsync_WhenCalled_InjectsRequirementsAndUiKitIntoSystemPrompt()
    {
        var harness = CreateHarness();
        AiSystemPrompt? captured = null;
        harness.Ai
            .Setup(service => service.StreamResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .Callback<AiSystemPrompt, IReadOnlyList<AiMessage>, CancellationToken>(
                (prompt, _, _) => captured = prompt)
            .Returns(PrototypeDemoHtmlAssertions.AsAsyncStream(GoldenModelHtml));

        await GenerateAsync(harness);

        Assert.NotNull(captured);
        var combinedPrompt = captured!.StablePart + captured.MutablePart;
        PrototypeDemoHtmlAssertions.AssertSystemPromptContains(combinedPrompt, RequirementMarker);
    }

    [Fact]
    public async Task GenerateAsync_WhenCalled_LoadsRequirementsForProject()
    {
        var harness = CreateHarness();

        await GenerateAsync(harness);

        harness.Artefacts.Verify(
            repo => repo.GetByProjectIdAsync(ProjectId, It.IsAny<CancellationToken>()),
            Times.Once);
        harness.Storage.Verify(
            store => store.GetContentAsync(RequirementS3Key, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Contract-preservation tests (six Day 0 harness checks) ----

    [Fact]
    public async Task GenerateAsync_WithCompliantModelOutput_PreservesExactPrototypeBanner()
    {
        var html = await GenerateAsync(CreateHarness());

        PrototypeDemoHtmlAssertions.AssertContainsExactPrototypeBanner(html);
    }

    [Fact]
    public async Task GenerateAsync_WithCompliantModelOutput_ReturnsCompleteHtmlDocument()
    {
        var html = await GenerateAsync(CreateHarness());

        PrototypeDemoHtmlAssertions.AssertCompleteHtmlDocument(html);
    }

    [Fact]
    public async Task GenerateAsync_WithCompliantModelOutput_ContainsNoPlausibleNhsNumbers()
    {
        var html = await GenerateAsync(CreateHarness());

        PrototypeDemoHtmlAssertions.AssertNoPlausibleNhsNumbers(html);
    }

    [Fact]
    public async Task GenerateAsync_WhenEmisXMode_ContainsNoExternalCdnReferences()
    {
        var html = await GenerateAsync(CreateHarness());

        PrototypeDemoHtmlAssertions.AssertNoExternalCdnReferences(html);
    }

    [Fact]
    public async Task GenerateAsync_WithCompliantModelOutput_RespectsScreenCountBound()
    {
        var html = await GenerateAsync(CreateHarness());

        PrototypeDemoHtmlAssertions.AssertScreenCountWithinBound(html);
    }
}
