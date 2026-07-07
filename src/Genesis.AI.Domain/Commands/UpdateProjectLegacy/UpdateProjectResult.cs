namespace Genesis.AI.Domain.Commands.UpdateProject;

public sealed record UpdateProjectResult(
    Guid ProjectId,
    string? FigmaPatPlaintext);
