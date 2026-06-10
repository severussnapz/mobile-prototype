using MediatR;

namespace Genesis.AI.Domain.Commands.GenerateDpiaReport;

/// <summary>
/// Generates a PR1625 DPIA Word document for a project from its structured
/// DPIA JSON artefact and persists it as a versioned artefact.
/// </summary>
public sealed record GenerateDpiaReportCommand(Guid ProjectId, string UserId)
    : IRequest<GenerateDpiaReportResult>;
