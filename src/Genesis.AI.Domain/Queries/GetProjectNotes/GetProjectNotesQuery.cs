using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjectNotes;

public record GetProjectNotesQuery(Guid ProjectId) : IRequest<IReadOnlyList<ProjectNote>?>;
