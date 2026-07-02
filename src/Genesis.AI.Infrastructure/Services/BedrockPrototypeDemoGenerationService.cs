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
/// Prompt cache split (Decision A):
///   Stable  = base generation prompt + emis-x-ui-kit.md (identical on every emis-x call — cached ~10× cheaper)
///   Mutable = project requirements only (per-project, always fresh)
/// </summary>
public sealed class BedrockPrototypeDemoGenerationService : IPrototypeDemoGenerationService
{
    private const string PromptResourceName =
        "Genesis.AI.Infrastructure.Prompts.PrototypeDemoGeneration.md";

    private const string UiKitResourceName =
        "Genesis.AI.Infrastructure.Resources.emis-x-ui-kit.md";

    private const string BaseCssResourceName =
        "Genesis.AI.Infrastructure.Resources.emis-x-base.css";

    private readonly IAiService _aiService;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _storageService;

    public BedrockPrototypeDemoGenerationService(
        IAiService aiService,
        IArtefactRepository artefactRepository,
        IArtefactStorageService storageService)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    }

    public async IAsyncEnumerable<string> GenerateAsync(
        Guid projectId,
        string projectName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var prompt = LoadEmbeddedText(PromptResourceName);
        var uiKit = LoadEmbeddedText(UiKitResourceName);
        var css = LoadEmbeddedText(BaseCssResourceName);

        var requirements = await LoadRequirementsAsync(projectId, cancellationToken);

        // Cache split A: stable = prompt + emis-x-ui-kit.md (shared across every emis-x call, cached ~10× cheaper);
        // mutable = requirements only (per-project, always fresh).
        var systemPrompt = new AiSystemPrompt(
            StablePart: BuildStablePart(prompt, uiKit),
            MutablePart: BuildMutablePart(requirements, projectName));

        var userMessage = new AiMessage(MessageRole.User, $"Generate a prototype demo for project: {projectName}");

        // Buffer the full model output — CSS injection requires a complete <head> section.
        // ponytail: single-chunk yield; upgrading to head-first streaming requires a prompt
        // change instructing the model to emit body-only content.
        var buffer = new StringBuilder();
        await foreach (var chunk in _aiService.StreamResponseAsync(systemPrompt, [userMessage], cancellationToken))
        {
            buffer.Append(chunk);
        }

        yield return InjectCssIntoHead(buffer.ToString(), css);
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

        return builder.ToString();
    }

    private static string BuildStablePart(string prompt, string uiKit)
    {
        return $"""
            {prompt}

            ## EMIS-X Design System Reference

            {uiKit}
            """;
    }

    private static string BuildMutablePart(string requirements, string projectName)
    {
        return $"""
            ## Project Requirements — {projectName}

            {requirements}
            """;
    }

    private static string InjectCssIntoHead(string html, string css)
    {
        const string closingHead = "</head>";
        var index = html.IndexOf(closingHead, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            // ponytail: malformed model output — no </head> found; return as-is so the
            // controller still surfaces the model's content rather than throwing.
            return html;
        }

        return html.Insert(index, $"<style>\n{css}\n</style>\n");
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
