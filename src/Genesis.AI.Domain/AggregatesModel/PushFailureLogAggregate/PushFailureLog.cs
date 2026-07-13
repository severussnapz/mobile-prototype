namespace Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate;

public sealed class PushFailureLog
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid ArtefactId { get; private set; }
    public string FilePath { get; private set; } = null!;
    public string ErrorMessage { get; private set; } = null!;
    public DateTimeOffset FailedAt { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    private PushFailureLog() { } // EF Core

    public PushFailureLog(
        Guid projectId, Guid artefactId, string filePath,
        string errorMessage, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        ArgumentNullException.ThrowIfNull(timeProvider);

        Id = Guid.NewGuid();
        ProjectId = projectId;
        ArtefactId = artefactId;
        FilePath = filePath;
        ErrorMessage = errorMessage;
        FailedAt = timeProvider.GetUtcNow();
        RetryCount = 0;
        ResolvedAt = null;
    }
}
