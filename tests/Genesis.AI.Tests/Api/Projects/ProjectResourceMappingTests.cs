using System.Text.Json;
using AutoMapper;
using Genesis.AI.Api.Features.Projects;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Tests.Api.Projects;

public sealed class ProjectResourceMappingTests
{
    private readonly IMapper _mapper;

    public ProjectResourceMappingTests()
    {
        var mapperConfig = new MapperConfiguration(configuration =>
            configuration.AddProfile<ProjectMappingProfile>());
        _mapper = mapperConfig.CreateMapper();
    }

    [Fact]
    public void ProjectResource_NeverExposesFigmaPatEncrypted()
    {
        var project = new Project(
            "DOC",
            "Documents",
            "A project",
            "PORTASK0001045",
            ComplianceDomain.ClinicalUk,
            "user-1",
            TimeProvider.System);

        project.UpdateP00Configuration(
            "EMIS Web",
            true,
            "Pilot process",
            true,
            true,
            true,
            false,
            "https://www.figma.com/file/abc123/Test",
            "secret-ciphertext",
            TimeProvider.System);

        var resource = _mapper.Map<ProjectResource>(project);
        var json = JsonSerializer.Serialize(resource);

        Assert.DoesNotContain("secret-ciphertext", json, StringComparison.Ordinal);
        Assert.DoesNotContain("figmaPatEncrypted", json, StringComparison.Ordinal);
        Assert.DoesNotContain("FigmaPatEncrypted", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectResource_FigmaPatConfigured_TrueWhenEncryptedSet()
    {
        var project = new Project(
            "DOC",
            "Documents",
            "A project",
            "PORTASK0001045",
            ComplianceDomain.ClinicalUk,
            "user-1",
            TimeProvider.System);

        project.UpdateP00Configuration(
            "EMIS Web",
            true,
            "Pilot process",
            true,
            true,
            true,
            false,
            "https://www.figma.com/file/abc123/Test",
            "any-value",
            TimeProvider.System);

        var resource = _mapper.Map<ProjectResource>(project);

        Assert.True(resource.FigmaPatConfigured);
    }

    [Fact]
    public void ProjectResource_FigmaPatConfigured_FalseWhenEncryptedNull()
    {
        var project = new Project(
            "DOC",
            "Documents",
            "A project",
            "PORTASK0001045",
            ComplianceDomain.ClinicalUk,
            "user-1",
            TimeProvider.System);

        project.UpdateP00Configuration(
            "EMIS Web",
            true,
            "Pilot process",
            true,
            true,
            true,
            false,
            "https://www.figma.com/file/abc123/Test",
            null,
            TimeProvider.System);

        var resource = _mapper.Map<ProjectResource>(project);

        Assert.False(resource.FigmaPatConfigured);
    }
}