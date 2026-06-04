using AutoMapper;
using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;

namespace Genesis.AI.Api.Features.Decisions;

public class DecisionMappingProfile : Profile
{
    public DecisionMappingProfile()
    {
        CreateMap<ProjectDecision, DecisionResource>();
    }
}
