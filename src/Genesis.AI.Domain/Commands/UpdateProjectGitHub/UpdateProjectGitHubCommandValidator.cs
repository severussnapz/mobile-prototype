using FluentValidation;

namespace Genesis.AI.Domain.Commands.UpdateProjectGitHub;

public sealed class UpdateProjectGitHubCommandValidator : AbstractValidator<UpdateProjectGitHubCommand>
{
    public UpdateProjectGitHubCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();

        RuleFor(command => command.TriggeredBy)
            .NotEmpty();

        RuleFor(command => command.GitHubApiRepoUrl)
            .Must(BeValidAbsoluteUrl)
            .When(command => command.GitHubApiRepoUrl is not null)
            .WithMessage("GitHubApiRepoUrl must be a valid absolute URL.");

        RuleFor(command => command.GitHubAppRepoUrl)
            .Must(BeValidAbsoluteUrl)
            .When(command => command.GitHubAppRepoUrl is not null)
            .WithMessage("GitHubAppRepoUrl must be a valid absolute URL.");

        RuleFor(command => command.FigmaFileUrl)
            .Must(BeValidAbsoluteUrl)
            .When(command => command.FigmaFileUrl is not null)
            .WithMessage("FigmaFileUrl must be a valid absolute URL.");
    }

    private static bool BeValidAbsoluteUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
