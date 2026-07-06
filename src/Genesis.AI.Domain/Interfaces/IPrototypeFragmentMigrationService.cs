namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Migrates a monolithic prototype/index.html into fragment files under prototype/fragments/.
/// Runs once at Prototype pipeline chat initialisation. Pure C#, no LLM, deterministic.
/// Detection signal: prototype/fragments/_shell.html existence in S3.
/// </summary>
public interface IPrototypeFragmentMigrationService
{
    /// <summary>
    /// Checks whether migration is needed and performs it if so.
    /// - _shell.html exists → already fragmented → returns Migrated=false
    /// - index.html missing → STATE 2 new project → returns Migrated=false
    /// - index.html exists, _shell.html missing → migrates → returns Migrated=true
    /// </summary>
    Task<PrototypeFragmentMigrationResult> MigrateIfNeededAsync(
        Guid projectId,
        string initiatedBy,
        CancellationToken cancellationToken);
}
