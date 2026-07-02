using System.Net;
using System.Text;
using System.Text.Json;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.IntegrationTests.Tests;

// Day 3b harness: HTTP/auth/serialisation contract for the Plan-4 prototype-demo
// EDIT endpoint (PrototypeDemoEditController — not yet implemented).
//
// RED for the right reason: the only production type missing is the controller.
// The build-anchor fact below references PrototypeDemoEditController directly, so
// the IntegrationTests project fails to compile ONLY on that missing type. Nothing
// else new is referenced — the locked response is asserted by parsing JSON, not by
// referencing a not-yet-written response DTO.
//
// Locked contract (already consumed by the committed app, api/prototypeDemo.ts +
// PrototypeDemoPage.tsx — NOT open for reconsideration):
//   POST /api/v1/projects/{projectId}/prototype-demo/edit
//   request  { selectedOuterHtml, instruction, activeUiKit }
//   response { data: { status, updatedOuterHtml, rejectionReason } }
//   status ∈ { "Applied", "OutOfScope", "NeedsClarification", "Rejected" } — verbatim.
//
// Auth: ProjectWrite — mirrors the committed PrototypeDemoController (generation)
// [Authorize(Policy = AuthorisationPolicies.ProjectWrite)] and the Day 2a review.
//
// Decisions confirmed with the user (both recommended option A):
//   - No project-existence check: the edit service is stateless (its own XML doc:
//     "No artefact repository or storage service is required — v1 is stateless"),
//     and the 4-status result has no ProjectNotFound. Any GUID routes; no project row
//     is created. So these tests do NOT seed a project.
//   - Malformed/empty body: framework model binding only. No custom field-level 400
//     guard (that would need a body shape outside the locked contract); the service
//     already returns Rejected for unusable input.
public class PrototypeDemoEditApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public PrototypeDemoEditApiTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    // Build-anchor: compiles (and the whole project goes green) only once the
    // controller type exists. This is the single intended compile failure.
    [Fact]
    public void PrototypeDemoEditController_TypeExists()
    {
        Assert.NotNull(typeof(Genesis.AI.Api.Features.PrototypeDemo.PrototypeDemoEditController));
    }

    private static StringContent EditBody(
        string selectedOuterHtml = "<button id=\"save\">Save</button>",
        string instruction = "make it say Submit",
        string activeUiKit = "emis-x")
    {
        var json = JsonSerializer.Serialize(new
        {
            selectedOuterHtml,
            instruction,
            activeUiKit
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private void SetupEditResult(PrototypeElementEditResult result)
    {
        _factory.PrototypeDemoEditServiceMock
            .Setup(service => service.EditElementAsync(
                It.IsAny<Guid>(),
                It.IsAny<PrototypeElementEditRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    // --- Auth: mirrors the committed PrototypeDemoApiTests smoke pattern (auth/routing only). ---

    [Fact]
    public async Task EditElement_WithWriteScope_ReturnsOk()
    {
        SetupEditResult(PrototypeElementEditResult.Applied("<button id=\"save\">Submit</button>"));
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/edit", EditBody());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task EditElement_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/edit", EditBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- One test per response status: exact field names + verbatim status casing. ---

    [Fact]
    public async Task EditElement_WhenServiceReturnsApplied_SerialisesAppliedWithNullRejectionReason()
    {
        SetupEditResult(PrototypeElementEditResult.Applied("<button id=\"save\">Submit</button>"));
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/edit", EditBody());
        var data = await ReadDataAsync(response);

        Assert.Equal("Applied", data.GetProperty("status").GetString());
        Assert.Equal("<button id=\"save\">Submit</button>", data.GetProperty("updatedOuterHtml").GetString());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("rejectionReason").ValueKind);
    }

    [Fact]
    public async Task EditElement_WhenServiceReturnsOutOfScope_SerialisesOutOfScopeVerbatim()
    {
        SetupEditResult(PrototypeElementEditResult.OutOfScope(
            "<button id=\"save\">Save</button>", "Cannot satisfy by editing this element alone."));
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/edit", EditBody());
        var data = await ReadDataAsync(response);

        Assert.Equal("OutOfScope", data.GetProperty("status").GetString());
        Assert.Equal("<button id=\"save\">Save</button>", data.GetProperty("updatedOuterHtml").GetString());
        Assert.Equal("Cannot satisfy by editing this element alone.", data.GetProperty("rejectionReason").GetString());
    }

    [Fact]
    public async Task EditElement_WhenServiceReturnsNeedsClarification_SerialisesNeedsClarificationVerbatim()
    {
        SetupEditResult(PrototypeElementEditResult.NeedsClarification(
            "<button id=\"save\">Save</button>", "Which button did you mean?"));
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/edit", EditBody());
        var data = await ReadDataAsync(response);

        Assert.Equal("NeedsClarification", data.GetProperty("status").GetString());
        Assert.Equal("<button id=\"save\">Save</button>", data.GetProperty("updatedOuterHtml").GetString());
        Assert.Equal("Which button did you mean?", data.GetProperty("rejectionReason").GetString());
    }

    [Fact]
    public async Task EditElement_WhenServiceReturnsRejected_SerialisesRejectedVerbatim()
    {
        SetupEditResult(PrototypeElementEditResult.Rejected("Model wrapped the element in prose."));
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/edit", EditBody());
        var data = await ReadDataAsync(response);

        Assert.Equal("Rejected", data.GetProperty("status").GetString());
        Assert.Equal("Model wrapped the element in prose.", data.GetProperty("rejectionReason").GetString());
    }

    // --- Request-shape: { selectedOuterHtml, instruction, activeUiKit } binds into the domain request. ---

    [Fact]
    public async Task EditElement_BindsRequestBodyIntoPrototypeElementEditRequest()
    {
        PrototypeElementEditRequest? captured = null;
        _factory.PrototypeDemoEditServiceMock
            .Setup(service => service.EditElementAsync(
                It.IsAny<Guid>(),
                It.IsAny<PrototypeElementEditRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, PrototypeElementEditRequest, CancellationToken>(
                (_, request, _) => captured = request)
            .ReturnsAsync(PrototypeElementEditResult.Applied("<button id=\"save\">Submit</button>"));

        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/edit",
            EditBody(
                selectedOuterHtml: "<span class=\"label\">Total</span>",
                instruction: "make it bold",
                activeUiKit: "emis-x"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(captured);
        Assert.Equal("<span class=\"label\">Total</span>", captured!.SelectedOuterHtml);
        Assert.Equal("make it bold", captured.Instruction);
        Assert.Equal("emis-x", captured.ActiveUiKit);
    }

    // --- Empty/whitespace selectedOuterHtml is NOT guarded away: it reaches the service and
    //     surfaces as a Rejected result (decision A — no custom field-level 400). The controller
    //     must not short-circuit and must not 500 on unusable input. ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EditElement_WhenSelectedOuterHtmlIsBlank_ReachesServiceAndReturnsRejected(string blankHtml)
    {
        PrototypeElementEditRequest? captured = null;
        _factory.PrototypeDemoEditServiceMock
            .Setup(service => service.EditElementAsync(
                It.IsAny<Guid>(),
                It.IsAny<PrototypeElementEditRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, PrototypeElementEditRequest, CancellationToken>(
                (_, request, _) => captured = request)
            .ReturnsAsync(PrototypeElementEditResult.Rejected("Selected element HTML was empty."));

        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/edit",
            EditBody(selectedOuterHtml: blankHtml));
        var data = await ReadDataAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(captured);
        Assert.Equal(blankHtml, captured!.SelectedOuterHtml);
        Assert.Equal("Rejected", data.GetProperty("status").GetString());
        Assert.Equal("Selected element HTML was empty.", data.GetProperty("rejectionReason").GetString());
    }

    // --- The projectId route segment is forwarded to the service (no project row required). ---

    [Fact]
    public async Task EditElement_ForwardsRouteProjectIdToService()
    {
        var projectId = Guid.NewGuid();
        Guid capturedProjectId = Guid.Empty;
        _factory.PrototypeDemoEditServiceMock
            .Setup(service => service.EditElementAsync(
                It.IsAny<Guid>(),
                It.IsAny<PrototypeElementEditRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, PrototypeElementEditRequest, CancellationToken>(
                (id, _, _) => capturedProjectId = id)
            .ReturnsAsync(PrototypeElementEditResult.Applied("<button id=\"save\">Submit</button>"));

        var client = _factory.CreateAdminClient();

        await client.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/edit", EditBody());

        Assert.Equal(projectId, capturedProjectId);
    }

    private static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("data").Clone();
    }
}
