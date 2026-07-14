using MediatR;

namespace Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;

/// <summary>
/// Raised when an artefact becomes published — either born-published on creation or
/// promoted from draft. Handled post-dispatch to index the artefact content into the
/// project knowledge namespace (pgvector). Fires from within the aggregate so the side
/// effect is bound to the artefact's own unit of work, immune to the DbContext/scope
/// mismatches that plagued the previous ChangeTracker-sniffing interceptor.
/// </summary>
public sealed record ArtefactPublishedDomainEvent(
    Guid ProjectId,
    string FilePath,
    string S3Key,
    string ContentType) : INotification;
