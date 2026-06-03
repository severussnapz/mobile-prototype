using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.CreateProject;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _projectRepository;
    private readonly TimeProvider _timeProvider;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (await _projectRepository.ExistsByCodeAsync(request.Code, cancellationToken))
        {
            throw new InvalidOperationException(
                $"A project with code '{request.Code}' already exists.");
        }

        var project = new Project(
            request.Code,
            request.Name,
            request.Description,
            request.ComplianceDomain,
            request.CreatedBy,
            _timeProvider);

        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
