using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class ContractManifestContextBuilder : IContractManifestContextBuilder
{
    private const string FilePath = "design/CONTRACT-MANIFEST.md";

    private static readonly HashSet<StageType> ConsumingStages =
    [
        StageType.Design,
        StageType.ClinicalSafety,
        StageType.InformationGovernance,
        StageType.Security
    ];

    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;

    public ContractManifestContextBuilder(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
    }

    public async Task<string> BuildContractManifestContextAsync(
        Guid projectId,
        StageType stageType,
        CancellationToken cancellationToken)
    {
        if (!ConsumingStages.Contains(stageType))
        {
            return string.Empty;
        }

        var artefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            projectId,
            FilePath,
            cancellationToken);

        if (artefact is null)
        {
            return string.Empty;
        }

        var content = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return $"## Contract Manifest\n{content}";
    }
}
