using FluentValidation;

namespace Genesis.AI.Domain.Commands.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    private static readonly HashSet<string> ValidReleaseTypes =
        new(StringComparer.OrdinalIgnoreCase) { "EMIS Web", "EMIS-X" };

    public UpdateProjectCommandValidator()
    {
        When(command => command.GitHubApiRepoUrl is not null, () =>
        {
            RuleFor(command => command.GitHubApiRepoUrl)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
                .WithMessage("GitHubApiRepoUrl must be a valid https://github.com URL.");
        });

        When(command => command.ReleaseType is not null, () =>
        {
            RuleFor(command => command.ReleaseType)
                .Must(releaseType => ValidReleaseTypes.Contains(releaseType!))
                .WithMessage("ReleaseType must be 'EMIS Web' or 'EMIS-X'.");
        });
    }
}
