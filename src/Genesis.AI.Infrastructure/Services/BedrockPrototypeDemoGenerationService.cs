using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Bedrock-backed implementation of <see cref="IPrototypeDemoGenerationService"/>.
/// Loads project requirements from S3, builds a system prompt from the embedded
/// <c>PrototypeDemoGeneration.md</c> base prompt and the EMIS-X UI kit, calls
/// <see cref="IAiService.StreamResponseAsync"/>, then inlines <c>emis-x-base.css</c>
/// into the model's <c>&lt;head&gt;</c> before yielding the final HTML document.
///
/// Prompt cache split (Decision C):
///   Stable  = base generation prompt (small, shared across every project)
///   Mutable = EMIS-X UI kit + project requirements (per-project, always fresh)
/// </summary>
public sealed class BedrockPrototypeDemoGenerationService : IPrototypeDemoGenerationService
{
    private static readonly TimeSpan StreamChunkTimeout = TimeSpan.FromSeconds(60);

    private const string PromptResourceName =
        "Genesis.AI.Infrastructure.Prompts.PrototypeDemoGeneration.md";

    private const string UiKitResourceName =
        "Genesis.AI.Infrastructure.Resources.emis-x-ui-kit.md";

    private readonly IAiService _aiService;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _storageService;
    private readonly IPrototypeDocumentAssembler _assembler;

    public BedrockPrototypeDemoGenerationService(
        IAiService aiService,
        IArtefactRepository artefactRepository,
        IArtefactStorageService storageService,
        IPrototypeDocumentAssembler assembler)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
    }

    public async IAsyncEnumerable<string> GenerateAsync(
        Guid projectId,
        string projectName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var prompt = LoadEmbeddedText(PromptResourceName);
        var uiKit = LoadEmbeddedText(UiKitResourceName);

        var requirements = await LoadRequirementsAsync(projectId, cancellationToken);

        // Cache split C: stable = prompt only (shared, small); mutable = ui-kit + requirements (per-project).
        var systemPrompt = new AiSystemPrompt(
            StablePart: prompt,
            MutablePart: BuildMutablePart(uiKit, requirements, projectName));

        var userMessage = new AiMessage(MessageRole.User, $"Generate a prototype demo for project: {projectName}");

        // Buffer the full model output — CSS injection requires a complete <head> section.
        // ponytail: single-chunk yield; upgrading to head-first streaming requires a prompt
        // change instructing the model to emit body-only content.
        var buffer = new StringBuilder();
        await using var streamEnumerator = _aiService
            .StreamResponseAsync(systemPrompt, [userMessage], cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            bool hasNext;
            try
            {
                hasNext = await streamEnumerator
                    .MoveNextAsync()
                    .AsTask()
                    .WaitAsync(StreamChunkTimeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException(
                    $"Bedrock stream produced no chunks for {StreamChunkTimeout.TotalSeconds:0} seconds.");
            }

            if (!hasNext)
            {
                break;
            }

            buffer.Append(streamEnumerator.Current);
        }

        yield return _assembler.Assemble(buffer.ToString());
    }

    public async IAsyncEnumerable<string> StreamRawAsync(
        Guid projectId,
        string projectName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var prompt = LoadEmbeddedText(PromptResourceName);
        var uiKit = LoadEmbeddedText(UiKitResourceName);

        var requirements = await LoadRequirementsAsync(projectId, cancellationToken);

        var systemPrompt = new AiSystemPrompt(
            StablePart: BuildStablePart(prompt, uiKit),
            MutablePart: BuildMutablePart(requirements, projectName));

        var userMessage = new AiMessage(MessageRole.User, $"Generate a prototype demo for project: {projectName}");

        await using var streamEnumerator = _aiService
            .StreamResponseAsync(systemPrompt, [userMessage], cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            bool hasNext;
            try
            {
                hasNext = await streamEnumerator
                    .MoveNextAsync()
                    .AsTask()
                    .WaitAsync(StreamChunkTimeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException(
                    $"Bedrock stream produced no chunks for {StreamChunkTimeout.TotalSeconds:0} seconds.");
            }

            if (!hasNext)
            {
                break;
            }

            yield return streamEnumerator.Current;
        }
    }

    private async Task<string> LoadRequirementsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var artefacts = await _artefactRepository.GetByProjectIdAsync(projectId, cancellationToken);

        var requirementArtefacts = artefacts
            .Where(artefact =>
                artefact.FilePath.StartsWith("requirements/REQ-", StringComparison.OrdinalIgnoreCase)
                && artefact.FilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var builder = new StringBuilder();
        foreach (var artefact in requirementArtefacts)
        {
            var content = await _storageService.GetContentAsync(artefact.S3Key, cancellationToken);
            if (content is not null)
            {
                builder.AppendLine(content);
            }
        }

        if (requirementArtefacts.Count > 0 && builder.Length == 0)
        {
            throw new InvalidOperationException(
                $"No requirement artefact content is available in S3 for project '{projectId}'.");
        }

        return builder.ToString();
    }

    private static string BuildMutablePart(string uiKit, string requirements, string projectName)
    {
        return $"""
            ## EMIS-X Design System Reference

            {uiKit}

            ## Project Requirements — {projectName}

            {requirements}
            """;
    }

    private static string LoadEmbeddedText(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
