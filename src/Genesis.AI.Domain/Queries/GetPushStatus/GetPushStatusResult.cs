namespace Genesis.AI.Domain.Queries.GetPushStatus;

public sealed record GetPushStatusResult(int UnresolvedCount, IReadOnlyList<Guid> FailedArtefactIds);