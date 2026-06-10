using AutoMapper;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;

namespace Genesis.AI.Api.Features.Projects;

public class ProjectMappingProfile : Profile
{
    public ProjectMappingProfile()
    {
        CreateMap<Project, ProjectResource>()
            .ForMember(
                destination => destination.PipelineStages,
                options => options.MapFrom(source => source.PipelineStages.OrderBy(stage => stage.SortOrder)))
            .ForMember(
                destination => destination.ComplianceDomain,
                options => options.MapFrom(source => ConvertToKebabCase(source.ComplianceDomain.ToString())))
            .ForMember(
                destination => destination.Status,
                options => options.MapFrom(source => ConvertToKebabCase(source.Status.ToString())));

        CreateMap<PipelineStage, PipelineStageResource>()
            .ForMember(
                destination => destination.StageType,
                options => options.MapFrom(source => ConvertToSnakeCase(source.StageType.ToString())))
            .ForMember(
                destination => destination.Status,
                options => options.MapFrom(source => ConvertToKebabCase(source.Status.ToString())));
    }

    private static string ConvertToKebabCase(string value)
    {
        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $"-{character}" : character.ToString()))
            .ToLowerInvariant();
    }

    private static string ConvertToSnakeCase(string value)
    {
        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $"_{character}" : character.ToString()))
            .ToLowerInvariant();
    }
}
