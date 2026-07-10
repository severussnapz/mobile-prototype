using Genesis.AI.Domain.Commands.GenerateSessionClose;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class SessionCloseContextBuilder : ISessionCloseContextBuilder
{
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;

    public SessionCloseContextBuilder(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
    }

    public async Task<string> BuildSessionCloseContextAsync(
        Guid projectId,
        StageType stageType,
        CancellationToken cancellationToken)
    {
        string filePath;
        try
        {
            filePath = SessionCloseStageMap.GetFilePath(stageType);
        }
        catch (NotSupportedException)
        {
            return string.Empty;
        }

        var artefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            projectId,
            filePath,
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

        return $"## SESSION RESUME (from last session close)\n\n{content}";
    }
}