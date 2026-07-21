using System.Text.Json;
using Genesis.AI.Api.Features.Normalisation;

namespace Genesis.AI.Tests.Api.Normalisation;

public sealed class NormalisationStatusResponseSerialiseTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void NormalisationStatusResponse_SerialisesAllFields()
    {
        // Arrange
        var artefact = new NormalisationArtefactResponse
        {
            ArtefactId = Guid.NewGuid(),
            FilePath = "output/REQ-001/checks.json",
            Version = 3,
            UpdatedAt = DateTimeOffset.Parse("2026-07-21T09:00:00+00:00")
        };

        var response = new NormalisationStatusResponse
        {
            RunStatus = "completed",
            LastRunAtUtc = DateTimeOffset.Parse("2026-07-21T09:30:00+00:00"),
            RunErrors = ["missing optional source"],
            GatePassed = true,
            PlanningEligible = true,
            BypassActive = false,
            BypassedBy = "admin-user",
            BypassedAtUtc = DateTimeOffset.Parse("2026-07-21T09:15:00+00:00"),
            GateErrors = ["none"],
            OutputArtefacts = [artefact]
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);

        // Assert
        var root = JsonDocument.Parse(json).RootElement;

        Assert.True(root.TryGetProperty("runStatus", out var runStatusElement), "runStatus field missing");
        Assert.Equal(response.RunStatus, runStatusElement.GetString());

        Assert.True(root.TryGetProperty("lastRunAtUtc", out var lastRunAtUtcElement), "lastRunAtUtc field missing");
        Assert.Equal(response.LastRunAtUtc, lastRunAtUtcElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("runErrors", out var runErrorsElement), "runErrors field missing");
        Assert.Equal("missing optional source", runErrorsElement.EnumerateArray().Single().GetString());

        Assert.True(root.TryGetProperty("gatePassed", out var gatePassedElement), "gatePassed field missing");
        Assert.Equal(response.GatePassed, gatePassedElement.GetBoolean());

        Assert.True(root.TryGetProperty("planningEligible", out var planningEligibleElement), "planningEligible field missing");
        Assert.Equal(response.PlanningEligible, planningEligibleElement.GetBoolean());

        Assert.True(root.TryGetProperty("bypassActive", out var bypassActiveElement), "bypassActive field missing");
        Assert.Equal(response.BypassActive, bypassActiveElement.GetBoolean());

        Assert.True(root.TryGetProperty("bypassedBy", out var bypassedByElement), "bypassedBy field missing");
        Assert.Equal(response.BypassedBy, bypassedByElement.GetString());

        Assert.True(root.TryGetProperty("bypassedAtUtc", out var bypassedAtUtcElement), "bypassedAtUtc field missing");
        Assert.Equal(response.BypassedAtUtc, bypassedAtUtcElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("gateErrors", out var gateErrorsElement), "gateErrors field missing");
        Assert.Equal("none", gateErrorsElement.EnumerateArray().Single().GetString());

        Assert.True(root.TryGetProperty("outputArtefacts", out var outputArtefactsElement), "outputArtefacts field missing");
        var artefactElement = outputArtefactsElement.EnumerateArray().Single();
        Assert.True(artefactElement.TryGetProperty("artefactId", out var artefactIdElement), "outputArtefacts.artefactId field missing");
        Assert.Equal(artefact.ArtefactId, artefactIdElement.GetGuid());
        Assert.True(artefactElement.TryGetProperty("filePath", out var filePathElement), "outputArtefacts.filePath field missing");
        Assert.Equal(artefact.FilePath, filePathElement.GetString());
        Assert.True(artefactElement.TryGetProperty("version", out var versionElement), "outputArtefacts.version field missing");
        Assert.Equal(artefact.Version, versionElement.GetInt32());
        Assert.True(artefactElement.TryGetProperty("updatedAt", out var updatedAtElement), "outputArtefacts.updatedAt field missing");
        Assert.Equal(artefact.UpdatedAt, updatedAtElement.GetDateTimeOffset());
    }
}
