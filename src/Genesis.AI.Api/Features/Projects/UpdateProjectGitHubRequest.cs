namespace Genesis.AI.Api.Features.Projects;

public sealed class UpdateProjectGitHubRequest
{
    public string? GitHubApiRepoUrl { get; init; }
    public string? GitHubAppRepoUrl { get; init; }
    public string? FigmaFileUrl { get; init; }
    public string? FigmaPat { get; init; }
}
