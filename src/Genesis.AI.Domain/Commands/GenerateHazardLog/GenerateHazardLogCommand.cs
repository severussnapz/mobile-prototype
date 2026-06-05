using MediatR;

namespace Genesis.AI.Domain.Commands.GenerateHazardLog;

/// <summary>
/// Generates a clinical safety hazard log spreadsheet for a project from its
/// hazard registry (<c>requirements/HAZARD-REGISTRY.md</c>) and persists it as a
/// versioned artefact.
/// </summary>
public record GenerateHazardLogCommand(Guid ProjectId, string UserId)
    : IRequest<GenerateHazardLogResult>;
