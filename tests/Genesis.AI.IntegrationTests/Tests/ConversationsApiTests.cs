using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.IntegrationTests.Tests;

public class ConversationsApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public ConversationsApiTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static async Task<(string ProjectId, string StageId)> CreateProjectAndGetFirstStageAsync(HttpClient client)
    {
        var content = new StringContent(
            """{"code":"CONV","name":"Conv Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        var projectId = data.GetProperty("id").GetString()!;
        var firstStage = data.GetProperty("pipelineStages").EnumerateArray().First();
        var stageId = firstStage.GetProperty("id").GetString()!;
        return (projectId, stageId);
    }

    private static async Task<(string ProjectId, string RequirementsStageId, string PrototypeStageId)> CreateProjectAndGetPipelineStageIdsAsync(HttpClient client)
    {
        var content = new StringContent(
            """{"code":"P02","name":"Pipeline02 Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");

        var requirementsStageId = string.Empty;
        var prototypeStageId = string.Empty;

        foreach (var stage in data.GetProperty("pipelineStages").EnumerateArray())
        {
            var stageType = stage.GetProperty("stageType").GetString();
            if (string.Equals(stageType, "requirements_discovery", StringComparison.OrdinalIgnoreCase))
            {
                requirementsStageId = stage.GetProperty("id").GetString()!;
            }

            if (string.Equals(stageType, "prototype", StringComparison.OrdinalIgnoreCase))
            {
                prototypeStageId = stage.GetProperty("id").GetString()!;
            }
        }

        return (
            data.GetProperty("id").GetString()!,
            requirementsStageId,
            prototypeStageId);
    }

    private static async Task<string> CreateConversationAsync(HttpClient client, string stageId)
    {
        var createConversationContent = new StringContent(
            $$$"""{"stageId":"{{{stageId}}}"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var createConversationResponse = await client.PostAsync("/api/v1/conversations", createConversationContent);
        var createConversationBody = await createConversationResponse.Content.ReadAsStringAsync();
        using var createConversationDoc = JsonDocument.Parse(createConversationBody);
        return createConversationDoc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    private static async Task<(string ProjectId, string PrototypeConversationId)> PreparePrototypeConversationAsync(HttpClient client)
    {
        var (projectId, requirementsStageId, prototypeStageId) = await CreateProjectAndGetPipelineStageIdsAsync(client);

        var requirementsConversationId = await CreateConversationAsync(client, requirementsStageId);
        Assert.NotNull(requirementsConversationId);

        var artefactPayload = new StringContent(
            """{"artefacts":[{"filePath":"requirements/REQ-001.md","contentType":"text/markdown","content":"# Requirement"}]}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var saveArtefactResponse = await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", artefactPayload);
        Assert.Equal(HttpStatusCode.Created, saveArtefactResponse.StatusCode);

        var completeRequirementsResponse = await client.PostAsync($"/api/v1/stages/{requirementsStageId}/complete", content: null);
        Assert.Equal(HttpStatusCode.OK, completeRequirementsResponse.StatusCode);

        var prototypeConversationId = await CreateConversationAsync(client, prototypeStageId);
        return (projectId, prototypeConversationId);
    }

    private static async Task SeedRequirementArtefactAsync(HttpClient client, string projectId, string filePath, string content)
    {
        var payload = new
        {
            artefacts = new[]
            {
                new { filePath, contentType = "text/markdown", content }
            }
        };
        var artefactPayload = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");
        var saveArtefactResponse = await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", artefactPayload);
        Assert.Equal(HttpStatusCode.Created, saveArtefactResponse.StatusCode);
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

    [Fact]
    public async Task CreateConversation_WithValidStageId_Returns201Created()
    {
        var client = _factory.CreateAdminClient();
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync(client);

        var content = new StringContent(
            $$$"""{"stageId":"{{{stageId}}}"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/conversations", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task GetConversation_WithValidId_ReturnsConversation()
    {
        var client = _factory.CreateAdminClient();
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync(client);

        // Create conversation
        var createContent = new StringContent(
            $$$"""{"stageId":"{{{stageId}}}"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var createResponse = await client.PostAsync("/api/v1/conversations", createContent);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createBody);
        var conversationId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        // Get it
        var response = await client.GetAsync($"/api/v1/conversations/{conversationId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task GetConversation_WithNonExistentId_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync($"/api/v1/conversations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetConversationsByStage_WithValidStageId_ReturnsConversations()
    {
        var client = _factory.CreateAdminClient();
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync(client);

        // Create a conversation
        var createContent = new StringContent(
            $$$"""{"stageId":"{{{stageId}}}"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        await client.PostAsync("/api/v1/conversations", createContent);

        // List by stage
        var response = await client.GetAsync($"/api/v1/conversations/by-stage/{stageId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        Assert.True(data.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task CreateConversation_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();
        var content = new StringContent(
            $$$"""{"stageId":"{{{Guid.NewGuid()}}}"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/conversations", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StreamAiResponse_Pipeline02SaveArtefact_SavesAndRetrievesPrototypeHtml()
    {
        var client = _factory.CreateAdminClient();
        var (projectId, prototypeConversationId) = await PreparePrototypeConversationAsync(client);

        using var savePrototypeToolInput = JsonDocument.Parse(
            """
            {
              "file_path": "prototype/index.html",
              "content": "<!doctype html><html><head><title>Prototype</title></head><body><h1>Prototype</h1><script id=\"prototype-metadata\" type=\"application/json\">{\"contractVersion\":\"1.0\",\"stageCode\":\"prototype\",\"generatedAtUtc\":\"2026-06-08T10:00:00Z\",\"prototypeOnly\":true,\"requirementsCovered\":[\"REQ-001\"],\"flows\":[\"Primary flow\"],\"privacySafetyConstraints\":[\"No real patient data\"]}</script></body></html>"
            }
            """);

        _factory.AiServiceMock
            .SetupSequence(service => service.StreamWithToolsAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<IReadOnlyList<AiToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateStreamEvents(
            [
                new AiToolCall("save_artefact", "tool-use-1", savePrototypeToolInput)
            ]))
            .Returns(CreateStreamEvents(
            [
                new AiTextChunk("Prototype saved.")
            ]));

        var streamRequest = new StringContent(
            """{"content":"build prototype"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var streamResponse = await client.PostAsync($"/api/v1/conversations/{prototypeConversationId}/stream", streamRequest);
        _ = await streamResponse.Content.ReadAsStringAsync();

        var artefactsResponse = await client.GetAsync($"/api/v1/projects/{projectId}/artefacts");
        var artefactsBody = await artefactsResponse.Content.ReadAsStringAsync();
        using var artefactsDocument = JsonDocument.Parse(artefactsBody);
        var prototypeArtefact = artefactsDocument.RootElement
            .EnumerateArray()
            .First(artefact => artefact.GetProperty("filePath").GetString() == "prototype/index.html");

        var artefactId = prototypeArtefact.GetProperty("id").GetString()!;
        var getArtefactResponse = await client.GetAsync($"/api/v1/projects/{projectId}/artefacts/{artefactId}");
        var getArtefactBody = await getArtefactResponse.Content.ReadAsStringAsync();
        using var artefactDocument = JsonDocument.Parse(getArtefactBody);
        var artefactContent = artefactDocument.RootElement.GetProperty("content").GetString();

        Assert.Equal(HttpStatusCode.OK, streamResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getArtefactResponse.StatusCode);
        Assert.NotNull(artefactContent);
        Assert.Contains("prototype-metadata", artefactContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamAiResponse_Pipeline02CompletionGate_MissingRequiredArtefactsDoesNotAdvancePhase()
    {
        var client = _factory.CreateAdminClient();
        var (_, prototypeConversationId) = await PreparePrototypeConversationAsync(client);

        using var advancePhaseToolInput = JsonDocument.Parse(
            """
            {
              "phase_number": 6,
              "phase_name": "completion"
            }
            """);

        _factory.AiServiceMock
            .SetupSequence(service => service.StreamWithToolsAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<IReadOnlyList<AiToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateStreamEvents(
            [
                new AiToolCall("advance_phase", "tool-use-advance", advancePhaseToolInput)
            ]))
            .Returns(CreateStreamEvents(
            [
                new AiTextChunk("Completion blocked.")
            ]));

        var streamRequest = new StringContent(
            """{"content":"complete prototype"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var streamResponse = await client.PostAsync($"/api/v1/conversations/{prototypeConversationId}/stream", streamRequest);
        _ = await streamResponse.Content.ReadAsStringAsync();

        var progressResponse = await client.GetAsync($"/api/v1/conversations/{prototypeConversationId}/progress");
        var progressBody = await progressResponse.Content.ReadAsStringAsync();
        using var progressDocument = JsonDocument.Parse(progressBody);

        Assert.Equal(HttpStatusCode.OK, streamResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, progressResponse.StatusCode);
        Assert.Equal(0, progressDocument.RootElement.GetProperty("currentPhase").GetInt32());
    }

    [Fact]
    public async Task StreamAiResponse_InvalidToolPayload_RetriesAndFailsClosedWithReason()
    {
        var client = _factory.CreateAdminClient();
        var (_, prototypeConversationId) = await PreparePrototypeConversationAsync(client);

        using var invalidSaveToolInput = JsonDocument.Parse("{}");

        _factory.AiServiceMock
            .Setup(service => service.StreamWithToolsAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<IReadOnlyList<AiToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateStreamEvents(
            [
                new AiToolCall("save_artefact", "tool-use-invalid", invalidSaveToolInput)
            ]));

        var streamRequest = new StringContent(
            """{"content":"save invalid"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var streamResponse = await client.PostAsync($"/api/v1/conversations/{prototypeConversationId}/stream", streamRequest);
        var streamBody = await streamResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, streamResponse.StatusCode);
        Assert.Contains("event: error", streamBody, StringComparison.Ordinal);
        Assert.Contains("Tool execution failed", streamBody, StringComparison.Ordinal);
        Assert.Contains("\"retryCount\":3", streamBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamAiResponse_WhenToolLoopReaches55Turns_EmitsNearLimitEvent()
    {
        var client = _factory.CreateAdminClient();
        var (_, prototypeConversationId) = await PreparePrototypeConversationAsync(client);

        // The loop limit is 60 turns. near_limit fires when turnsRemaining drops to 5 (i.e. after turn 55 of 60).
        using var listArtefactsInput = JsonDocument.Parse("{}");

        var sequence = _factory.AiServiceMock
            .SetupSequence(service => service.StreamWithToolsAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<IReadOnlyList<AiToolDefinition>>(),
                It.IsAny<CancellationToken>()));

        // 55 turns of tool calls (brings turnsRemaining to 5, triggering the near_limit warning)
        for (var turn = 0; turn < 55; turn++)
        {
            sequence = sequence.Returns(CreateStreamEvents(
            [
                new AiToolCall("list_artefacts", $"tool-use-{turn}", listArtefactsInput)
            ]));
        }

        // Turn 56+ returns text to break the loop
        sequence.Returns(CreateStreamEvents([new AiTextChunk("Done.")]));

        var streamRequest = new StringContent(
            """{"content":"list artefacts many times"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var streamResponse = await client.PostAsync($"/api/v1/conversations/{prototypeConversationId}/stream", streamRequest);
        var streamBody = await streamResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, streamResponse.StatusCode);
        Assert.Contains("event: near_limit", streamBody, StringComparison.Ordinal);
        Assert.Contains("\"turnsRemaining\":5", streamBody, StringComparison.Ordinal);
        Assert.DoesNotContain("event: tool_limit_hit", streamBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamAiResponse_WhenToolLoopExhaustsAllTurns_EmitsToolLimitHitEvent()
    {
        var client = _factory.CreateAdminClient();
        var (_, prototypeConversationId) = await PreparePrototypeConversationAsync(client);

        using var listArtefactsInput = JsonDocument.Parse("{}");

        var sequence = _factory.AiServiceMock
            .SetupSequence(service => service.StreamWithToolsAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<IReadOnlyList<AiToolDefinition>>(),
                It.IsAny<CancellationToken>()));

        // 60 turns of tool calls — exhausts the loop completely (default max is 60)
        for (var turn = 0; turn < 60; turn++)
        {
            sequence = sequence.Returns(CreateStreamEvents(
            [
                new AiToolCall("list_artefacts", $"tool-use-{turn}", listArtefactsInput)
            ]));
        }

        var streamRequest = new StringContent(
            """{"content":"loop forever"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var streamResponse = await client.PostAsync($"/api/v1/conversations/{prototypeConversationId}/stream", streamRequest);
        var streamBody = await streamResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, streamResponse.StatusCode);
        Assert.Contains("event: near_limit", streamBody, StringComparison.Ordinal);
        Assert.Contains("event: tool_limit_hit", streamBody, StringComparison.Ordinal);
        Assert.Contains("\"turnsUsed\":60", streamBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamAiResponse_WhenAdvanceRequirementCalledWithoutRequirementArtefact_BlocksCompletionWithGateError()
    {
        var client = _factory.CreateAdminClient();
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync(client);

        // No requirement artefact seeded — gate must block
        var conversationPayload = new StringContent(
            $$$"""{"stageId":"{{{stageId}}}","requirementId":"REQ-001"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var createConversationResponse = await client.PostAsync("/api/v1/conversations", conversationPayload);
        var createConversationBody = await createConversationResponse.Content.ReadAsStringAsync();
        using var createConversationDoc = JsonDocument.Parse(createConversationBody);
        var conversationId = createConversationDoc.RootElement.GetProperty("data").GetProperty("id").GetString()!;

        using var advanceRequirementInput = JsonDocument.Parse("""{"requirement_id":"REQ-001","summary":"Done"}""");

        _factory.AiServiceMock
            .SetupSequence(service => service.StreamWithToolsAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<IReadOnlyList<AiToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            // Turn 1: AI attempts to advance without saving — gate error returned as tool result
            .Returns(CreateStreamEvents(
            [
                new AiToolCall("advance_requirement", "tool-use-advance", advanceRequirementInput)
            ]))
            // Turn 2: AI acknowledges and stops
            .Returns(CreateStreamEvents([new AiTextChunk("I need to save the artefact first.")]));

        var streamRequest = new StringContent(
            """{"content":"mark this requirement done"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var streamResponse = await client.PostAsync($"/api/v1/conversations/{conversationId}/stream", streamRequest);
        var streamBody = await streamResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, streamResponse.StatusCode);
        // Gate blocked — requirement_complete event must not be emitted
        Assert.DoesNotContain("event: requirement_complete", streamBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamAiResponse_WhenAdvanceRequirementCalledWithExistingRequirementArtefact_EmitsRequirementCompleteEvent()
    {
        var client = _factory.CreateAdminClient();
        var (projectId, stageId) = await CreateProjectAndGetFirstStageAsync(client);

        // Pre-seed requirement artefact via REST endpoint (simulates artefact saved in a prior session)
        await SeedRequirementArtefactAsync(
            client,
            projectId,
            "requirements/REQ-001_patient_search.md",
            "# REQ-001\n\nCapture patient search requirement.");

        var conversationPayload = new StringContent(
            $$$"""{"stageId":"{{{stageId}}}","requirementId":"REQ-001"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var createConversationResponse = await client.PostAsync("/api/v1/conversations", conversationPayload);
        var createConversationBody = await createConversationResponse.Content.ReadAsStringAsync();
        using var createConversationDoc = JsonDocument.Parse(createConversationBody);
        var conversationId = createConversationDoc.RootElement.GetProperty("data").GetProperty("id").GetString()!;

        using var advanceRequirementInput = JsonDocument.Parse("""{"requirement_id":"REQ-001","summary":"Patient search captured"}""");

        _factory.AiServiceMock
            .SetupSequence(service => service.StreamWithToolsAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<IReadOnlyList<AiToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateStreamEvents(
            [
                new AiToolCall("advance_requirement", "tool-use-advance", advanceRequirementInput)
            ]))
            .Returns(CreateStreamEvents([new AiTextChunk("Requirement REQ-001 complete.")]));

        var streamRequest = new StringContent(
            """{"content":"mark this requirement done"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var streamResponse = await client.PostAsync($"/api/v1/conversations/{conversationId}/stream", streamRequest);
        var streamBody = await streamResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, streamResponse.StatusCode);
        Assert.Contains("event: requirement_complete", streamBody, StringComparison.Ordinal);
        Assert.DoesNotContain("requirement_completion_gate_failed", streamBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamAiResponse_FullConversationHistory_AllMessagesPassedToAiService()
    {
        var client = _factory.CreateAdminClient();
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync(client);
        var conversationId = await CreateConversationAsync(client, stageId);

        IReadOnlyList<AiMessage>? capturedMessages = null;

        _factory.AiServiceMock
            .Setup(service => service.StreamWithToolsAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<IReadOnlyList<AiToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .Callback<AiSystemPrompt, IReadOnlyList<AiMessage>, IReadOnlyList<AiToolDefinition>, CancellationToken>(
                (_, messages, _, _) => capturedMessages = messages)
            .Returns(CreateStreamEvents(
            [
                new AiTextChunk("Understood.")
            ]));

        for (var questionNumber = 1; questionNumber <= 6; questionNumber++)
        {
            var streamRequest = new StringContent(
                $"{{\"content\":\"Question {questionNumber}\"}}",
                System.Text.Encoding.UTF8,
                "application/json");

            var streamResponse = await client.PostAsync(
                $"/api/v1/conversations/{conversationId}/stream",
                streamRequest);
            _ = await streamResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, streamResponse.StatusCode);
        }

        Assert.NotNull(capturedMessages);
        Assert.True(capturedMessages!.Count >= 6);
        Assert.Contains(
            capturedMessages,
            message => message.Content.Contains("Question 1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StreamAiResponse_ContractManifestArtefactExists_ManifestContentPresentInPrompt()
    {
        var client = _factory.CreateAdminClient();
        var projectContent = new StringContent(
            """{"code":"P04","name":"Pipeline04 Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var projectResponse = await client.PostAsync("/api/v1/projects", projectContent);
        var projectBody = await projectResponse.Content.ReadAsStringAsync();
        using var projectDocument = JsonDocument.Parse(projectBody);
        var projectData = projectDocument.RootElement.GetProperty("data");
        var projectId = projectData.GetProperty("id").GetString()!;
        var pipelineStages = projectData.GetProperty("pipelineStages").EnumerateArray().ToArray();
        var requirementsStageId = pipelineStages
            .First(stage => string.Equals(
                stage.GetProperty("stageType").GetString(),
                "requirements_discovery",
                StringComparison.OrdinalIgnoreCase))
            .GetProperty("id")
            .GetString()!;
        var prototypeStageId = pipelineStages
            .First(stage => string.Equals(
                stage.GetProperty("stageType").GetString(),
                "prototype",
                StringComparison.OrdinalIgnoreCase))
            .GetProperty("id")
            .GetString()!;
        var designStage = pipelineStages
            .First(stage => string.Equals(
                stage.GetProperty("stageType").GetString(),
                "design",
                StringComparison.OrdinalIgnoreCase));
        var designStageId = designStage.GetProperty("id").GetString()!;

        await SeedRequirementArtefactAsync(
            client,
            projectId,
            "requirements/REQ-001.md",
            "# Requirement");

        var requirementsConversationId = await CreateConversationAsync(client, requirementsStageId);
        Assert.NotNull(requirementsConversationId);

        var completeRequirementsResponse = await client.PostAsync($"/api/v1/stages/{requirementsStageId}/complete", content: null);
        Assert.Equal(HttpStatusCode.OK, completeRequirementsResponse.StatusCode);

        var prototypeConversationId = await CreateConversationAsync(client, prototypeStageId);
        Assert.NotNull(prototypeConversationId);

        var completePrototypeResponse = await client.PostAsync($"/api/v1/stages/{prototypeStageId}/complete", content: null);
        Assert.Equal(HttpStatusCode.OK, completePrototypeResponse.StatusCode);

        await SeedRequirementArtefactAsync(
            client,
            projectId,
            "design/CONTRACT-MANIFEST.md",
            """
            # Contract Manifest

            <!-- contract-manifest-version: 1 -->
            <!-- req-provenance: requirements/REQ-001.md@v1 -->
            <!-- arch-provenance: architecture/ARCH.md@v1 -->

            ## 1. Status Header
            Manifest version: 1

            ## 2. Pinned File Versions
            ...

            ## 3. Requirement Ledger
            ...

            ## 4. Shared Element Index
            ...

            ## 5. Reuse Log
            ...

            ## 6. TDD Gate (Plan 5)
            Gate open: NO
            """);

        var conversationId = await CreateConversationAsync(client, designStageId);

        AiSystemPrompt? capturedPrompt = null;

        _factory.AiServiceMock
            .Setup(service => service.StreamWithToolsAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<IReadOnlyList<AiToolDefinition>>(),
                It.IsAny<CancellationToken>()))
            .Callback<AiSystemPrompt, IReadOnlyList<AiMessage>, IReadOnlyList<AiToolDefinition>, CancellationToken>(
                (prompt, _, _, _) => capturedPrompt = prompt)
            .Returns(CreateStreamEvents(
            [
                new AiTextChunk("ok")
            ]));

        var streamRequest = new StringContent(
            """{"content":"design this requirement"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var streamResponse = await client.PostAsync($"/api/v1/conversations/{conversationId}/stream", streamRequest);
        _ = await streamResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, streamResponse.StatusCode);
        Assert.NotNull(capturedPrompt);
        Assert.Contains("Contract Manifest", capturedPrompt!.MutablePart, StringComparison.Ordinal);
        Assert.Contains("contract-manifest-version: 1", capturedPrompt.MutablePart, StringComparison.Ordinal);
    }
}
