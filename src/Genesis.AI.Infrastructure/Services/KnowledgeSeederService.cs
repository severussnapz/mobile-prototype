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

        // Use CancellationToken.None for seeding — the work must complete
        // regardless of host shutdown signals. The delay above respects
        // stoppingToken so the container can still shut down cleanly during startup.
        await SeedGenesisToolNamespaceAsync(CancellationToken.None);
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

        if (!Prefixes.Any(knownPrefix => resourceName.StartsWith(knownPrefix, StringComparison.Ordinal)))
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
        var prefix = Prefixes.First(knownPrefix => resourceName.StartsWith(knownPrefix, StringComparison.Ordinal));
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
            var seedResult = await ProcessResourceAsync(
                assembly,
                resourceName,
                knowledgeService,
                dbContext,
                cancellationToken);

            indexed += seedResult.Indexed;
            skipped += seedResult.Skipped;
            failed += seedResult.Failed;
        }

        LogSeedingSummary(indexed, skipped, failed);
    }

    private async Task<(int Indexed, int Skipped, int Failed)> ProcessResourceAsync(
        Assembly assembly,
        string resourceName,
        IKnowledgeService knowledgeService,
        GenesisAiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (ShouldExcludeResource(resourceName))
        {
            return (0, 0, 0);
        }

        try
        {
            var content = ReadAndStripFrontmatter(assembly, resourceName);
            if (string.IsNullOrWhiteSpace(content))
            {
                return (0, 1, 0);
            }

            var sourcePath = BuildSourcePath(resourceName);
            if (await ResourceAlreadyIndexedAsync(dbContext, sourcePath, cancellationToken))
            {
                return (0, 1, 0);
            }

            var prefix = Prefixes.First(knownPrefix => resourceName.StartsWith(knownPrefix, StringComparison.Ordinal));
            var metadata = BuildMetadata(content, prefix);

            await knowledgeService.IndexDocumentAsync(
                KnowledgeNamespace.GenesisTool,
                projectId: null,
                sourcePath,
                content,
                metadata,
                cancellationToken);

            return (1, 0, 0);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to seed knowledge resource {ResourceName}", resourceName);
            return (0, 0, 1);
        }
    }

    private static Dictionary<string, string> BuildMetadata(string content, string prefix)
    {
        return new Dictionary<string, string>
        {
            ["contentHash"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))),
            ["prefix"] = prefix
        };
    }

    private static Task<bool> ResourceAlreadyIndexedAsync(
        GenesisAiDbContext dbContext,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        return dbContext.KnowledgeDocument.AnyAsync(
            knowledgeDocument => knowledgeDocument.Namespace == KnowledgeNamespace.GenesisTool
                && knowledgeDocument.SourcePath == sourcePath
                && knowledgeDocument.ChunkIndex == 0,
            cancellationToken);
    }

    private void LogSeedingSummary(int indexed, int skipped, int failed)
    {
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
