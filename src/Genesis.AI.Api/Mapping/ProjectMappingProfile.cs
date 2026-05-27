using AutoMapper;
using Genesis.AI.Api.Resources;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;

namespace Genesis.AI.Api.Mapping;

public class ProjectMappingProfile : Profile
{
    public ProjectMappingProfile()
    {
        CreateMap<Project, ProjectResource>()
            .ForMember(
                destination => destination.ComplianceDomain,
                options => options.MapFrom(source => ConvertToKebabCase(source.ComplianceDomain.ToString())))
            .ForMember(
                destination => destination.Status,
                options => options.MapFrom(source => ConvertToKebabCase(source.Status.ToString())));

        CreateMap<PipelineStage, PipelineStageResource>()
            .ForMember(
                destination => destination.StageType,
                options => options.MapFrom(source => ConvertToKebabCase(source.StageType.ToString())))
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
}
