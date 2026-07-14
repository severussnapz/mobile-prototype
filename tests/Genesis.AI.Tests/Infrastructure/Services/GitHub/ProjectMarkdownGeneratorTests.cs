using System.Text.RegularExpressions;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Infrastructure.Services.GitHub;

namespace Genesis.AI.Tests.Infrastructure.Services.GitHub;

public sealed class ProjectMarkdownGeneratorTests
{
    private readonly ProjectMarkdownGenerator _generator = new();

    [Fact]
    public void Generate_ContainsProjectNameAndCode()
    {
        var project = CreateTestProject(name: "Test Project", code: "TST");

        var result = _generator.Generate(project);

        Assert.Contains("Test Project", result);
        Assert.Contains("TST", result);
    }

    [Fact]
    public void Generate_ContainsReleaseType()
    {
        var project = CreateTestProject();
        project.UpdateP00Configuration(
            releaseType: "EMIS-X",
            assuranceRequired: null,
            pilotDeploymentProcess: null,
            csoRoleAssigned: null,
            igOwnerRoleAssigned: null,
            securityReviewerAssigned: null,
            medicalDeviceFlag: null,
            figmaFileUrl: null,
            figmaPatEncrypted: null,
            timeProvider: TimeProvider.System);

        var result = _generator.Generate(project);

        Assert.Contains("EMIS-X", result);
    }

    [Fact]
    public void Generate_ContainsAssuranceRequired()
    {
        var project = CreateTestProject();
        project.UpdateP00Configuration(
            releaseType: null,
            assuranceRequired: true,
            pilotDeploymentProcess: null,
            csoRoleAssigned: null,
            igOwnerRoleAssigned: null,
            securityReviewerAssigned: null,
            medicalDeviceFlag: null,
            figmaFileUrl: null,
            figmaPatEncrypted: null,
            timeProvider: TimeProvider.System);

        var result = _generator.Generate(project);

        Assert.Contains("Yes", result);
    }

    [Fact]
    public void Generate_ContainsCsoRoleAssigned()
    {
        var project = CreateTestProject();
        project.UpdateP00Configuration(
            releaseType: null,
            assuranceRequired: null,
            pilotDeploymentProcess: null,
            csoRoleAssigned: true,
            igOwnerRoleAssigned: null,
            securityReviewerAssigned: null,
            medicalDeviceFlag: null,
            figmaFileUrl: null,
            figmaPatEncrypted: null,
            timeProvider: TimeProvider.System);

        var result = _generator.Generate(project);

        Assert.Contains("CSO Role Assigned: Yes", result);
    }

    [Fact]
    public void Generate_ContainsMedicalDeviceFlag()
    {
        var project = CreateTestProject();
        project.UpdateP00Configuration(
            releaseType: null,
            assuranceRequired: null,
            pilotDeploymentProcess: null,
            csoRoleAssigned: null,
            igOwnerRoleAssigned: null,
            securityReviewerAssigned: null,
            medicalDeviceFlag: false,
            figmaFileUrl: null,
            figmaPatEncrypted: null,
            timeProvider: TimeProvider.System);

        var result = _generator.Generate(project);

        Assert.Contains("Medical Device: No", result);
    }

    [Fact]
    public void Generate_NeverContainsErn()
    {
        var project = CreateTestProject();
        project.UpdateP00Configuration(
            releaseType: "EMIS-X",
            assuranceRequired: true,
            pilotDeploymentProcess: "Pilot via EMIS-X deployment pipeline",
            csoRoleAssigned: true,
            igOwnerRoleAssigned: true,
            securityReviewerAssigned: true,
            medicalDeviceFlag: false,
            figmaFileUrl: null,
            figmaPatEncrypted: null,
            timeProvider: TimeProvider.System);

        var result = _generator.Generate(project);

        Assert.DoesNotMatch(@"@[a-zA-Z0-9]+", result);
    }

    [Fact]
    public void Generate_NullableFieldsNotSet_DoesNotThrow()
    {
        var project = CreateTestProject();

        var exception = Record.Exception(() => _generator.Generate(project));

        Assert.Null(exception);
    }

    private static Project CreateTestProject(string name = "My Project", string code = "MYP")
    {
        var project = new Project(
            code,
            name,
            "A test project",
            "PORTASK0001045",
            ComplianceDomain.ClinicalUk,
            "system",
            TimeProvider.System);

        project.SetGitHubConfig(
            "https://github.com/emisgroup/emis-x-docs",
            "https://github.com/emisgroup/emis-x-docs-app",
            "emisgroup",
            "emis-x-docs",
            "144995615",
            TimeProvider.System);

        return project;
    }
}
