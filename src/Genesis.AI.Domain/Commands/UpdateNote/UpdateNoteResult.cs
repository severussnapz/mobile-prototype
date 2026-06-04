using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;

namespace Genesis.AI.Domain.Commands.UpdateNote;

public record UpdateNoteResult(bool Found, ProjectNote? Note = null);
