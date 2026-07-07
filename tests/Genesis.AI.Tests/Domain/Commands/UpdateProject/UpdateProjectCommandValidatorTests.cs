using FluentValidation.TestHelper;
using Genesis.AI.Domain.Commands.UpdateProject;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Tests.Domain.Commands.UpdateProject;

public sealed class UpdateProjectCommandValidatorTests
{
    private readonly UpdateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidGitHubUrl_PassesValidation()
    {
        var command = CreateCommand() with
        {
            GitHubApiRepoUrl = "https://github.com/emisgroup/emis-x-documents"
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(item => item.GitHubApiRepoUrl);
    }

    [Fact]
    public void Validate_InvalidGitHubUrl_FailsValidation()
    {
        var command = CreateCommand() with
        {
            GitHubApiRepoUrl = "not-a-url"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.GitHubApiRepoUrl);
    }

    [Fact]
    public void Validate_GitHubUrlNotGitHub_FailsValidation()
    {
        var command = CreateCommand() with
        {
            GitHubApiRepoUrl = "https://gitlab.com/emisgroup/emis-x-documents"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.GitHubApiRepoUrl);
    }

    [Fact]
    public void Validate_NullGitHubUrl_PassesValidation()
    {
        var command = CreateCommand() with
        {
            GitHubApiRepoUrl = null
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(item => item.GitHubApiRepoUrl);
    }

    [Fact]
    public void Validate_ReleaseTypeValid_PassesValidation()
    {
        var command = CreateCommand() with
        {
            ReleaseType = "EMIS Web"
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(item => item.ReleaseType);
    }

    [Fact]
    public void Validate_ReleaseTypeInvalid_FailsValidation()
    {
        var command = CreateCommand() with
        {
            ReleaseType = "something-invalid"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.ReleaseType);
    }

    private static UpdateProjectCommand CreateCommand()
    {
        return new UpdateProjectCommand(
            Guid.NewGuid(),
            "Updated name",
            "Updated description",
            "PORTASK0001045",
            ComplianceDomain.ClinicalUk,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "user-1");
    }
}