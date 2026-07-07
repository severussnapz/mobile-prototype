using FluentValidation;

namespace Genesis.AI.Domain.Commands.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    private static readonly HashSet<string> AllowedReleaseTypes =
    [
        "EMIS Web",
        "EMIS-X"
    ];

    public UpdateProjectCommandValidator()
    {
        RuleFor(command => command.GitHubApiRepoUrl)
            .Must(BeValidGitHubUrl)
            .When(command => command.GitHubApiRepoUrl is not null)
            .WithMessage("GitHub API repository URL must be a valid github.com URL.");

        RuleFor(command => command.ReleaseType)
            .Must(releaseType => releaseType is null || AllowedReleaseTypes.Contains(releaseType))
            .WithMessage("ReleaseType must be one of: EMIS Web, EMIS-X.");
    }

    private static bool BeValidGitHubUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps)
            && string.Equals(parsed.Host, "github.com", StringComparison.OrdinalIgnoreCase);
    }
}
