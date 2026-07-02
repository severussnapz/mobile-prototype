namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Generates a prototype-demo HTML page for a project. Returns the output as a
/// stream of string chunks so the controller can serve it synchronously (Day 1) or
/// forward chunks over SSE (Day 2) without modifying this contract.
/// </summary>
public interface IPrototypeDemoGenerationService
{
    IAsyncEnumerable<string> GenerateAsync(Guid projectId, string projectName, CancellationToken cancellationToken);
}
