namespace Genesis.AI.Api.Features.Projects;

public sealed record UpdateProjectResponse(
    Guid ProjectId,
    string? FigmaPatPlaintext
);
