namespace Genesis.AI.Domain.Commands.UpdateProjectGitHub;

public sealed record UpdateProjectGitHubResult(
    string? FigmaPatPlaintext,
    bool? ApiRepoVerified = null,
    string? ApiRepoError = null,
    bool? AppRepoVerified = null,
    string? AppRepoError = null
);
