using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Seeds the <see cref="KnowledgeNamespace.GenesisTool"/> knowledge namespace on
/// application startup by reading embedded .md resources (Prompts, Skills, KnowledgeBase)
/// from the infrastructure assembly and indexing them into pgvector.
///
/// Runs off the critical path (after a short delay) so it never blocks Kestrel.
/// Idempotent — a SHA-256 content hash is stored in metadata and used to skip
/// unchanged files. Best-effort — any per-file failure is logged and skipped.
/// </summary>
public sealed class KnowledgeSeederService : BackgroundService
{
    private const string AssemblyPrefix = "Genesis.AI.Infrastructure.";
    private const string PromptsPrefix = "Genesis.AI.Infrastructure.Prompts.";
    private const string SkillsPrefix = "Genesis.AI.Infrastructure.Skills.";
    private const string KnowledgeBasePrefix = "Genesis.AI.Infrastructure.KnowledgeBase.";
    private const string MarkdownSuffix = ".md";
    private const string ExcludedMarker = "_ai_new_tmp";

    private static readonly string[] Prefixes = [PromptsPrefix, SkillsPrefix, KnowledgeBasePrefix];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KnowledgeSeederService> _logger;

    public KnowledgeSeederService(
        IServiceScopeFactory scopeFactory,
        ILogger<KnowledgeSeederService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small delay to allow app to fully start before seeding
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        await SeedGenesisToolNamespaceAsync(stoppingToken);
    }

    /// <summary>
    /// Determines if a resource should be excluded from seeding.
    /// Excludes resources that:
    /// - Don't end with .md
    /// - Contain "_ai_new_tmp" marker
    /// - Don't start with a known prefix (Prompts, Skills, KnowledgeBase)
    /// </summary>
    internal static bool ShouldExcludeResource(string resourceName)
    {
        if (!resourceName.EndsWith(MarkdownSuffix, StringComparison.Ordinal))
        {
            return true;
        }

        if (resourceName.Contains(ExcludedMarker, StringComparison.Ordinal))
        {
            return true;
        }

        if (!Prefixes.Any(p => resourceName.StartsWith(p, StringComparison.Ordinal)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Converts an embedded resource name into a folder-style source path.
    /// The resource name uses '.' as a separator; the file name itself may contain
    /// hyphens but no dots, so the remainder after the known prefix is "{filename}.md".
    /// Example: "Genesis.AI.Infrastructure.Prompts.Pipeline01RequirementsDiscovery.md"
    /// → "Prompts/Pipeline01RequirementsDiscovery.md".
    /// </summary>
    internal static string BuildSourcePath(string resourceName)
    {
        var prefix = Prefixes.First(p => resourceName.StartsWith(p, StringComparison.Ordinal));
        var folder = prefix == PromptsPrefix ? "Prompts" :
                     prefix == SkillsPrefix ? "Skills" : "KnowledgeBase";
        var fileName = resourceName[prefix.Length..];
        return $"{folder}/{fileName}";
    }

    private async Task SeedGenesisToolNamespaceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<GenesisAiDbContext>();

        var assembly = Assembly.GetExecutingAssembly();

        var indexed = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            var prefix = Prefixes.FirstOrDefault(p => resourceName.StartsWith(p, StringComparison.Ordinal));
            if (prefix is null || !resourceName.EndsWith(MarkdownSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            if (resourceName.Contains(ExcludedMarker, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                var content = ReadAndStripFrontmatter(assembly, resourceName);
                if (string.IsNullOrWhiteSpace(content))
                {
                    skipped++;
                    continue;
                }

                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
                var sourcePath = BuildSourcePath(resourceName);

                var exists = await dbContext.KnowledgeDocument
                    .AnyAsync(k => k.Namespace == KnowledgeNamespace.GenesisTool
                        && k.SourcePath == sourcePath
                        && k.Metadata["contentHash"] == hash, cancellationToken);

                if (exists)
                {
                    skipped++;
                    continue;
                }

                await knowledgeService.IndexDocumentAsync(
                    KnowledgeNamespace.GenesisTool,
                    projectId: null,
                    sourcePath,
                    content,
                    new Dictionary<string, string>
                    {
                        ["contentHash"] = hash,
                        ["prefix"] = prefix
                    },
                    cancellationToken);

                indexed++;
            }
            catch (Exception exception)
            {
                failed++;
                _logger.LogWarning(exception, "Failed to seed knowledge resource {ResourceName}", resourceName);
            }
        }

        _logger.LogInformation(
            "Knowledge seeder complete: {Indexed} indexed, {Skipped} skipped, {Failed} failed",
            indexed,
            skipped,
            failed);
    }

    private static string ReadAndStripFrontmatter(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        // Strip YAML frontmatter if present
        if (content.StartsWith("---", StringComparison.Ordinal))
        {
            var endIndex = content.IndexOf("\n---", 3, StringComparison.Ordinal);
            if (endIndex > 0)
            {
                content = content[(endIndex + 4)..].TrimStart();
            }
        }

        return content;
    }
}
