using MediatR;

namespace Genesis.AI.Domain.Commands.DeleteProject;

public record DeleteProjectCommand(Guid ProjectId) : IRequest;
