using AutoMapper;
using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;

namespace Genesis.AI.Api.Features.Notes;

public class NoteMappingProfile : Profile
{
    public NoteMappingProfile()
    {
        CreateMap<ProjectNote, NoteResource>();
    }
}
