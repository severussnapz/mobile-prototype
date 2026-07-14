namespace Genesis.AI.Api.Features.Projects;

public sealed record UpdateProjectGitHubResponse(
    string? FigmaPatPlaintext,
    bool? ApiRepoVerified = null,
    string? ApiRepoError = null,
    bool? AppRepoVerified = null,
    string? AppRepoError = null
);
