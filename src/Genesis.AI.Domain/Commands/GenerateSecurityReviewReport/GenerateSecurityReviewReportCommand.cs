using MediatR;

namespace Genesis.AI.Domain.Commands.GenerateSecurityReviewReport;

/// <summary>
/// Generates the security review report for a project from its structured
/// security assurance and SDP evidence artefacts and persists it as a versioned
/// artefact.
/// </summary>
public sealed record GenerateSecurityReviewReportCommand(Guid ProjectId, string UserId)
    : IRequest<GenerateSecurityReviewReportResult>;