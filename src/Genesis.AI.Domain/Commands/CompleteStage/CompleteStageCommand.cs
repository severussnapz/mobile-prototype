using MediatR;

namespace Genesis.AI.Domain.Commands.CompleteStage;

public record CompleteStageCommand(Guid StageId, string UserId) : IRequest<CompleteStageResult>;
