using System.Text.Json;
using System.Text.RegularExpressions;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Normalisation;

namespace Genesis.AI.Infrastructure.Services;

public sealed class NormalisationGateService : INormalisationGateService
{
    private static readonly Regex RequirementFileRegex =
        new("^requirements/(?<id>REQ-\\d{3})(?:[^/]*)\\.md$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] RunBlockingPrerequisiteFiles =
    [
        "manifest.md"
    ];

    private static readonly string[] RequiredSourceArtefactsForGate =
    [
        "output/SECURITY_ASSURANCE_DATA.json",
        "output/SDP_EVIDENCE.json"
    ];

    private static readonly string[] RequiredNormalisationFiles =
    [
        "checks.json",
        "hazards.json",
        "api_contracts.json",
        "schema.json",
        "interfaces.json",
        "components.json",
        "observability.json"
    ];

    private static readonly string[] RequiredCrossCuttingFiles =
    [
        "output/cross_cutting/traceability.json",
        "output/cross_cutting/dependency_graph.json",
        "output/cross_cutting/last_extracted.json",
        "output/CS_Guardrails.json"
    ];

    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;

    public NormalisationGateService(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
    }

    public async Task<NormalisationGateEvaluation> EvaluateAsync(
        Guid projectId,
        string projectCode,
        CancellationToken cancellationToken)
    {
        var allArtefacts = await _artefactRepository.GetByProjectIdAsync(projectId, cancellationToken);

        var latestByPath = BuildLatestByPath(allArtefacts);
        var errors = new List<string>();

        var (runBlockingErrors, requirementArtefacts) = CheckRunPrerequisites(latestByPath);
        errors.AddRange(runBlockingErrors);

        CheckRequiredSourceArtefacts(latestByPath, errors);

        await ValidateRequirementOutputsAsync(latestByPath, requirementArtefacts, projectCode, errors, cancellationToken);

        await ValidateCrossCuttingFilesAsync(latestByPath, errors, cancellationToken);

        var outputArtefacts = BuildOutputArtefacts(latestByPath);

        var runPrerequisitesMet = runBlockingErrors.Count == 0;

        return new NormalisationGateEvaluation(
            runPrerequisitesMet,
            errors.Count == 0,
            errors,
            outputArtefacts);
    }

    private static Dictionary<string, Artefact> BuildLatestByPath(IReadOnlyList<Artefact> allArtefacts)
    {
        return allArtefacts
            .GroupBy(artefact => artefact.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                grouping => grouping.Key,
                grouping => grouping
                    .OrderByDescending(artefact => artefact.Version)
                    .ThenByDescending(artefact => artefact.CreatedAt)
                    .First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static (List<string> RunBlockingErrors, List<Artefact> RequirementArtefacts) CheckRunPrerequisites(
        Dictionary<string, Artefact> latestByPath)
    {
        var runBlockingErrors = new List<string>();

        foreach (var prerequisiteFile in RunBlockingPrerequisiteFiles)
        {
            if (!latestByPath.ContainsKey(prerequisiteFile))
            {
                runBlockingErrors.Add($"Missing run prerequisite artefact: {prerequisiteFile}");
            }
        }

        var requirementArtefacts = latestByPath.Values
            .Where(artefact => RequirementFileRegex.IsMatch(artefact.FilePath))
            .OrderBy(artefact => artefact.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requirementArtefacts.Count == 0)
        {
            runBlockingErrors.Add("No requirement source artefacts found under requirements/REQ-*.md.");
        }

        return (runBlockingErrors, requirementArtefacts);
    }

    private static void CheckRequiredSourceArtefacts(Dictionary<string, Artefact> latestByPath, List<string> errors)
    {
        foreach (var requiredSourceArtefact in RequiredSourceArtefactsForGate)
        {
            if (!latestByPath.ContainsKey(requiredSourceArtefact))
            {
                errors.Add($"Missing required source artefact for gate: {requiredSourceArtefact}");
            }
        }
    }

    private async Task ValidateRequirementOutputsAsync(
        Dictionary<string, Artefact> latestByPath,
        List<Artefact> requirementArtefacts,
        string projectCode,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        foreach (var requirementArtefact in requirementArtefacts)
        {
            var requirementId = RequirementFileRegex.Match(requirementArtefact.FilePath).Groups["id"].Value.ToUpperInvariant();
            var compactRequirementId = requirementId.Replace("-", string.Empty, StringComparison.Ordinal);

            var candidateDirectories = new[]
            {
                $"output/{requirementId}/",
                $"output/{compactRequirementId}/",
                $"output/{projectCode.ToUpperInvariant()}_{compactRequirementId}/"
            };

            var resolvedDirectory = candidateDirectories.FirstOrDefault(directory =>
                RequiredNormalisationFiles.All(requiredFile =>
                    latestByPath.ContainsKey($"{directory}{requiredFile}")));

            if (resolvedDirectory is null)
            {
                errors.Add($"Missing normalisation output set for {requirementId} (expected one of: {string.Join(", ", candidateDirectories)}).");
                continue;
            }

            foreach (var requiredFile in RequiredNormalisationFiles)
            {
                var filePath = $"{resolvedDirectory}{requiredFile}";
                if (!latestByPath.TryGetValue(filePath, out var artefact))
                {
                    errors.Add($"Missing required output file: {filePath}");
                    continue;
                }

                await ValidateJsonArtefactAsync(artefact, errors, cancellationToken);
            }
        }
    }

    private async Task ValidateCrossCuttingFilesAsync(
        Dictionary<string, Artefact> latestByPath,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        foreach (var crossCuttingFile in RequiredCrossCuttingFiles)
        {
            if (!latestByPath.TryGetValue(crossCuttingFile, out var artefact))
            {
                errors.Add($"Missing cross-cutting output file: {crossCuttingFile}");
                continue;
            }

            await ValidateJsonArtefactAsync(artefact, errors, cancellationToken);
        }
    }

    private static List<NormalisationArtefactSummary> BuildOutputArtefacts(Dictionary<string, Artefact> latestByPath)
    {
        return latestByPath.Values
            .Where(artefact => artefact.FilePath.StartsWith("output/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(artefact => artefact.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(artefact => artefact.Version)
            .Select(artefact => new NormalisationArtefactSummary(
                artefact.Id,
                artefact.FilePath,
                artefact.Version,
                artefact.CreatedAt))
            .ToList();
    }

    private async Task ValidateJsonArtefactAsync(
        Artefact artefact,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var content = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            errors.Add($"Output file is empty: {artefact.FilePath}");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                errors.Add($"Output file has invalid root JSON value: {artefact.FilePath}");
            }
        }
        catch (JsonException jsonException)
        {
            errors.Add($"Output file is not valid JSON: {artefact.FilePath} ({jsonException.Message})");
        }
    }
}
