using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.DeleteProject;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly TimeProvider _timeProvider;

    public DeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project with ID '{request.ProjectId}' was not found.");

        project.SoftDelete(_timeProvider);
        await _projectRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
