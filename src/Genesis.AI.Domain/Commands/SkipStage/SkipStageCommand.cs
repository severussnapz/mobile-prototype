using MediatR;

namespace Genesis.AI.Domain.Commands.SkipStage;

public record SkipStageCommand(Guid StageId) : IRequest<SkipStageResult>;
