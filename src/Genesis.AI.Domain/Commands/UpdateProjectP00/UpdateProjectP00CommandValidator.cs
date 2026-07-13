using FluentValidation;

namespace Genesis.AI.Domain.Commands.UpdateProjectP00;

public sealed class UpdateProjectP00CommandValidator : AbstractValidator<UpdateProjectP00Command>
{
    private static readonly HashSet<string> AllowedReleaseTypes =
    [
        "EMIS Web",
        "EMIS-X"
    ];

    public UpdateProjectP00CommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();

        RuleFor(command => command.TriggeredBy)
            .NotEmpty();

        RuleFor(command => command.ReleaseType)
            .Must(releaseType => releaseType is null || AllowedReleaseTypes.Contains(releaseType))
            .WithMessage("ReleaseType must be one of: EMIS Web, EMIS-X.");

        RuleFor(command => command.PilotDeploymentProcess)
            .MaximumLength(2000)
            .When(command => command.PilotDeploymentProcess is not null);
    }
}
