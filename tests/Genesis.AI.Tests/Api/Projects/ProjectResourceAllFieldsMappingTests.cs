using System.Text.Json;
using AutoMapper;
using Genesis.AI.Api.Features.Projects;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Tests.Api.Projects;

public sealed class ProjectResourceAllFieldsMappingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IMapper _mapper;

    public ProjectResourceAllFieldsMappingTests()
    {
        var mapperConfig = new MapperConfiguration(configuration =>
            configuration.AddProfile<ProjectMappingProfile>());
        _mapper = mapperConfig.CreateMapper();
    }

    [Fact]
    public void ProjectResource_AndPipelineStageResource_MapAndSerialiseAllFields()
    {
        // Arrange
        var timeProvider = TimeProvider.System;
        var project = new Project(
            "DOC",
            "Documents",
            "Mapped project",
            "PORTASK0001045",
            ComplianceDomain.ClinicalUk,
            "user-1",
            timeProvider);

        project.UpdateGitHubUrls(
            "https://github.com/org/api-repo",
            "https://github.com/org/app-repo",
            "https://www.figma.com/file/abc123/Prototype",
            timeProvider);

        project.UpdateP00Configuration(
            "EMIS X",
            true,
            "Pilot process",
            true,
            true,
            true,
            false,
            "https://www.figma.com/file/abc123/Prototype",
            "encrypted-token",
            timeProvider);

        var requirementsStage = project.PipelineStages.First(stage => stage.StageType == StageType.RequirementsDiscovery);
        requirementsStage.Start(timeProvider);
        requirementsStage.Complete("completer-1", timeProvider);
        project.RecalculateStatus(timeProvider);

        // Act
        var resource = _mapper.Map<ProjectResource>(project);
        var json = JsonSerializer.Serialize(resource, JsonOptions);

        // Assert
        var root = JsonDocument.Parse(json).RootElement;

        Assert.True(root.TryGetProperty("id", out var idElement), "id field missing");
        Assert.Equal(project.Id, idElement.GetGuid());

        Assert.True(root.TryGetProperty("code", out var codeElement), "code field missing");
        Assert.Equal(project.Code, codeElement.GetString());

        Assert.True(root.TryGetProperty("name", out var nameElement), "name field missing");
        Assert.Equal(project.Name, nameElement.GetString());

        Assert.True(root.TryGetProperty("description", out var descriptionElement), "description field missing");
        Assert.Equal(project.Description, descriptionElement.GetString());

        Assert.True(root.TryGetProperty("timeSheetCode", out var timeSheetCodeElement), "timeSheetCode field missing");
        Assert.Equal(project.TimeSheetCode, timeSheetCodeElement.GetString());

        Assert.True(root.TryGetProperty("complianceDomain", out var complianceDomainElement), "complianceDomain field missing");
        Assert.Equal(project.ComplianceDomain.ToString(), complianceDomainElement.GetString());

        Assert.True(root.TryGetProperty("status", out var statusElement), "status field missing");
        Assert.Equal("in-progress", statusElement.GetString());

        Assert.True(root.TryGetProperty("createdBy", out var createdByElement), "createdBy field missing");
        Assert.Equal(project.CreatedBy, createdByElement.GetString());

        Assert.True(root.TryGetProperty("createdAt", out var createdAtElement), "createdAt field missing");
        Assert.Equal(project.CreatedAt, createdAtElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("updatedAt", out var updatedAtElement), "updatedAt field missing");
        Assert.Equal(project.UpdatedAt, updatedAtElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("figmaPatConfigured", out var figmaPatConfiguredElement), "figmaPatConfigured field missing");
        Assert.True(figmaPatConfiguredElement.GetBoolean());

        Assert.True(root.TryGetProperty("gitHubApiRepoUrl", out var gitHubApiRepoUrlElement), "gitHubApiRepoUrl field missing");
        Assert.Equal(project.GitHubApiRepoUrl, gitHubApiRepoUrlElement.GetString());

        Assert.True(root.TryGetProperty("gitHubAppRepoUrl", out var gitHubAppRepoUrlElement), "gitHubAppRepoUrl field missing");
        Assert.Equal(project.GitHubAppRepoUrl, gitHubAppRepoUrlElement.GetString());

        Assert.True(root.TryGetProperty("releaseType", out var releaseTypeElement), "releaseType field missing");
        Assert.Equal(project.ReleaseType, releaseTypeElement.GetString());

        Assert.True(root.TryGetProperty("assuranceRequired", out var assuranceRequiredElement), "assuranceRequired field missing");
        Assert.Equal(project.AssuranceRequired, assuranceRequiredElement.GetBoolean());

        Assert.True(root.TryGetProperty("pilotDeploymentProcess", out var pilotDeploymentProcessElement), "pilotDeploymentProcess field missing");
        Assert.Equal(project.PilotDeploymentProcess, pilotDeploymentProcessElement.GetString());

        Assert.True(root.TryGetProperty("csoRoleAssigned", out var csoRoleAssignedElement), "csoRoleAssigned field missing");
        Assert.Equal(project.CsoRoleAssigned, csoRoleAssignedElement.GetBoolean());

        Assert.True(root.TryGetProperty("igOwnerRoleAssigned", out var igOwnerRoleAssignedElement), "igOwnerRoleAssigned field missing");
        Assert.Equal(project.IgOwnerRoleAssigned, igOwnerRoleAssignedElement.GetBoolean());

        Assert.True(root.TryGetProperty("securityReviewerAssigned", out var securityReviewerAssignedElement), "securityReviewerAssigned field missing");
        Assert.Equal(project.SecurityReviewerAssigned, securityReviewerAssignedElement.GetBoolean());

        Assert.True(root.TryGetProperty("figmaFileUrl", out var figmaFileUrlElement), "figmaFileUrl field missing");
        Assert.Equal(project.FigmaFileUrl, figmaFileUrlElement.GetString());

        Assert.True(root.TryGetProperty("figmaPatHint", out var figmaPatHintElement), "figmaPatHint field missing");
        Assert.Equal("••••••••", figmaPatHintElement.GetString());

        Assert.True(root.TryGetProperty("medicalDeviceFlag", out var medicalDeviceFlagElement), "medicalDeviceFlag field missing");
        Assert.Equal(project.MedicalDeviceFlag, medicalDeviceFlagElement.GetBoolean());

        Assert.True(root.TryGetProperty("pipelineStages", out var pipelineStagesElement), "pipelineStages field missing");
        var mappedStageElement = pipelineStagesElement
            .EnumerateArray()
            .First(stageElement => stageElement.GetProperty("id").GetGuid() == requirementsStage.Id);

        // PipelineStageResource field assertions (all 8 fields)
        Assert.True(mappedStageElement.TryGetProperty("id", out var stageIdElement), "pipelineStages.id field missing");
        Assert.Equal(requirementsStage.Id, stageIdElement.GetGuid());

        Assert.True(mappedStageElement.TryGetProperty("stageType", out var stageTypeElement), "pipelineStages.stageType field missing");
        Assert.Equal("requirements_discovery", stageTypeElement.GetString());

        Assert.True(mappedStageElement.TryGetProperty("status", out var stageStatusElement), "pipelineStages.status field missing");
        Assert.Equal("complete", stageStatusElement.GetString());

        Assert.True(mappedStageElement.TryGetProperty("iteration", out var iterationElement), "pipelineStages.iteration field missing");
        Assert.Equal(requirementsStage.Iteration, iterationElement.GetInt32());

        Assert.True(mappedStageElement.TryGetProperty("sortOrder", out var sortOrderElement), "pipelineStages.sortOrder field missing");
        Assert.Equal(requirementsStage.SortOrder, sortOrderElement.GetInt32());

        Assert.True(mappedStageElement.TryGetProperty("startedAt", out var startedAtElement), "pipelineStages.startedAt field missing");
        Assert.Equal(requirementsStage.StartedAt, startedAtElement.GetDateTimeOffset());

        Assert.True(mappedStageElement.TryGetProperty("completedAt", out var completedAtElement), "pipelineStages.completedAt field missing");
        Assert.Equal(requirementsStage.CompletedAt, completedAtElement.GetDateTimeOffset());

        Assert.True(mappedStageElement.TryGetProperty("completedBy", out var completedByElement), "pipelineStages.completedBy field missing");
        Assert.Equal(requirementsStage.CompletedBy, completedByElement.GetString());
    }
}
