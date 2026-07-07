using Genesis.AI.Domain.Enums;
using MediatR;

namespace Genesis.AI.Domain.Commands.GenerateSessionClose;

public sealed record GenerateSessionCloseCommand(
    Guid ProjectId,
    Guid ConversationId,
    StageType StageType,
    string UserErn
) : IRequest<GenerateSessionCloseResult>;