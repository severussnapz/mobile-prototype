using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;

namespace Genesis.AI.Domain.Commands.CreateNote;

public record CreateNoteResult(bool ProjectFound, ProjectNote? Note = null);
