using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Microsoft.Extensions.Time.Testing;

namespace Genesis.AI.Tests.Domain.ProjectAggregate;

public sealed class ProjectGitHubConfigTests
{
    [Fact]
    public void SetGitHubConfig_SetsAllProperties()
    {
        var timeProvider = new FakeTimeProvider();
        var project = new Project(
            "DOC",
            "Documents",
            "A project",
            "PORTASK0001045",
            ComplianceDomain.ClinicalUk,
            "user-1",
            timeProvider);

        var beforeUpdate = project.UpdatedAt;
        timeProvider.Advance(TimeSpan.FromSeconds(1));

        project.SetGitHubConfig(
            "https://github.com/emisgroup/emis-x-documents-api",
            "https://github.com/emisgroup/emis-x-documents-app",
            "emisgroup",
            "emis-x-documents",
            "12345678",
            timeProvider);

        Assert.Equal("https://github.com/emisgroup/emis-x-documents-api", project.GitHubApiRepoUrl);
        Assert.Equal("https://github.com/emisgroup/emis-x-documents-app", project.GitHubAppRepoUrl);
        Assert.Equal("emisgroup", project.GitHubRepoOwner);
        Assert.Equal("emis-x-documents", project.GitHubRepoName);
        Assert.Equal("12345678", project.GitHubInstallationId);
        Assert.True(project.UpdatedAt > beforeUpdate);
    }

    [Fact]
    public void SetGitHubConfig_NullInstallationId_ThrowsArgumentException()
    {
        var project = CreateProject(TimeProvider.System);

        Assert.ThrowsAny<ArgumentException>(() => project.SetGitHubConfig(
            "https://github.com/emisgroup/emis-x-documents-api",
            "https://github.com/emisgroup/emis-x-documents-app",
            "emisgroup",
            "emis-x-documents",
            null!,
            TimeProvider.System));
    }

    [Fact]
    public void HasGitHubConfig_WhenInstallationIdNull_ReturnsFalse()
    {
        var project = CreateProject(TimeProvider.System);

        Assert.False(project.HasGitHubConfig);
    }

    [Fact]
    public void HasGitHubConfig_WhenInstallationIdSet_ReturnsTrue()
    {
        var project = CreateProject(TimeProvider.System);

        project.SetGitHubConfig(
            "https://github.com/emisgroup/emis-x-documents-api",
            "https://github.com/emisgroup/emis-x-documents-app",
            "emisgroup",
            "emis-x-documents",
            "12345678",
            TimeProvider.System);

        Assert.True(project.HasGitHubConfig);
    }

    [Fact]
    public void UpdateP00Configuration_SetsAllProperties()
    {
        var timeProvider = new FakeTimeProvider();
        var project = CreateProject(timeProvider);
        var beforeUpdate = project.UpdatedAt;
        timeProvider.Advance(TimeSpan.FromSeconds(1));

        project.UpdateP00Configuration(
            "EMIS Web",
            true,
            "Pilot process",
            true,
            true,
            true,
            false,
            "https://www.figma.com/file/abc123/Test",
            "ciphertext",
            timeProvider);

        Assert.Equal("EMIS Web", project.ReleaseType);
        Assert.True(project.AssuranceRequired);
        Assert.Equal("Pilot process", project.PilotDeploymentProcess);
        Assert.True(project.CsoRoleAssigned);
        Assert.True(project.IgOwnerRoleAssigned);
        Assert.True(project.SecurityReviewerAssigned);
        Assert.False(project.MedicalDeviceFlag);
        Assert.Equal("https://www.figma.com/file/abc123/Test", project.FigmaFileUrl);
        Assert.Equal("ciphertext", project.FigmaPatEncrypted);
        Assert.True(project.UpdatedAt > beforeUpdate);
    }

    [Fact]
    public void UpdateP00Configuration_AllNullable_DoesNotThrow()
    {
        var project = CreateProject(TimeProvider.System);

        var exception = Record.Exception(() => project.UpdateP00Configuration(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            TimeProvider.System));

        Assert.Null(exception);
    }

    private static Project CreateProject(TimeProvider timeProvider)
    {
        return new Project(
            "DOC",
            "Documents",
            "A project",
            "PORTASK0001045",
            ComplianceDomain.ClinicalUk,
            "user-1",
            timeProvider);
    }
}