using MediatR;

namespace Genesis.AI.Domain.Commands.UpdateProjectGitHub;

public sealed record UpdateProjectGitHubCommand(
    Guid ProjectId,
    string TriggeredBy,
    string? GitHubApiRepoUrl,
    string? GitHubAppRepoUrl,
    string? FigmaFileUrl,
    string? FigmaPat,
    string? GitHubInstallationId
) : IRequest<UpdateProjectGitHubResult>;
