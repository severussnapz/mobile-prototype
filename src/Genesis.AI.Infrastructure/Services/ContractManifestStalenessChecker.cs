using System.Text.RegularExpressions;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class ContractManifestStalenessChecker : IContractManifestStalenessChecker
{
    private static readonly Regex ContractManifestVersionComment = new(
        @"<!--\s*contract-manifest-version:\s*(\d+)\s*-->",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ReqProvenanceComment = new(
        @"<!--\s*req-provenance:\s*(.*?)\s*-->",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ArchProvenanceComment = new(
        @"<!--\s*arch-provenance:\s*(.*?)\s*-->",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ProvenanceEntry = new(
        @"^(.*?)@v(\d+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IArtefactRepository _artefactRepository;

    public ContractManifestStalenessChecker(IArtefactRepository artefactRepository)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
    }

    public async Task<IReadOnlyList<string>> CheckStalenessAsync(
        Guid projectId,
        string manifestContent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manifestContent))
        {
            return [];
        }

        var hasManifestVersionComment = ContractManifestVersionComment.IsMatch(manifestContent);
        var reqProvenanceMatch = ReqProvenanceComment.Match(manifestContent);
        var archProvenanceMatch = ArchProvenanceComment.Match(manifestContent);

        if (!hasManifestVersionComment && !reqProvenanceMatch.Success && !archProvenanceMatch.Success)
        {
            return [];
        }

        var warnings = new List<string>();

        foreach (var entry in ParseReqEntries(reqProvenanceMatch))
        {
            var warning = await CheckEntryAsync(projectId, entry.FilePath, entry.PinnedVersion, cancellationToken);
            if (warning is not null)
            {
                warnings.Add(warning);
            }
        }

        var archEntry = ParseSingleEntry(archProvenanceMatch);
        if (archEntry is not null)
        {
            var warning = await CheckEntryAsync(projectId, archEntry.Value.FilePath, archEntry.Value.PinnedVersion, cancellationToken);
            if (warning is not null)
            {
                warnings.Add(warning);
            }
        }

        return warnings;
    }

    private async Task<string?> CheckEntryAsync(
        Guid projectId,
        string filePath,
        int pinnedVersion,
        CancellationToken cancellationToken)
    {
        var artefact = await _artefactRepository.GetByProjectAndFilePathAsync(projectId, filePath, cancellationToken);
        if (artefact is null)
        {
            return $"⚠️ CONTRACT STALE: {filePath} is missing — re-run P04";
        }

        if (artefact.Version != pinnedVersion)
        {
            return $"⚠️ CONTRACT STALE: CONTRACT-MANIFEST.md was produced against {filePath}@v{pinnedVersion} but current approved version is v{artefact.Version}. Re-run P04 for this requirement before proceeding.";
        }

        return null;
    }

    private static List<(string FilePath, int PinnedVersion)> ParseReqEntries(Match reqProvenanceMatch)
    {
        if (!reqProvenanceMatch.Success)
        {
            return [];
        }

        var rawValue = reqProvenanceMatch.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return [];
        }

        var entries = new List<(string FilePath, int PinnedVersion)>();
        foreach (var entry in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parsedEntry = ParseEntry(entry);
            if (parsedEntry is not null)
            {
                entries.Add(parsedEntry.Value);
            }
        }

        return entries;
    }

    private static (string FilePath, int PinnedVersion)? ParseSingleEntry(Match provenanceMatch)
    {
        if (!provenanceMatch.Success)
        {
            return null;
        }

        var rawValue = provenanceMatch.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        return ParseEntry(rawValue);
    }

    private static (string FilePath, int PinnedVersion)? ParseEntry(string rawEntry)
    {
        var match = ProvenanceEntry.Match(rawEntry.Trim());
        if (!match.Success)
        {
            return null;
        }

        var filePath = match.Groups[1].Value.Trim();
        var pinnedVersion = int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        return (filePath, pinnedVersion);
    }
}