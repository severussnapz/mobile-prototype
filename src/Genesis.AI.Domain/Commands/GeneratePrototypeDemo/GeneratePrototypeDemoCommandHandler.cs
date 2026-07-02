using System.Text;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.GeneratePrototypeDemo;

/// <summary>
/// Handles <see cref="GeneratePrototypeDemoCommand"/>: loads the project, collects
/// the streaming HTML from <see cref="IPrototypeDemoGenerationService"/> into a
/// single string, and returns it. The service returns <see cref="IAsyncEnumerable{T}"/>
/// so the controller can switch to SSE forwarding on Day 2 without changing this
/// handler.
/// </summary>
public sealed class GeneratePrototypeDemoCommandHandler
    : IRequestHandler<GeneratePrototypeDemoCommand, GeneratePrototypeDemoResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IPrototypeDemoGenerationService _generationService;

    public GeneratePrototypeDemoCommandHandler(
        IProjectRepository projectRepository,
        IPrototypeDemoGenerationService generationService)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
    }

    public async Task<GeneratePrototypeDemoResult> Handle(
        GeneratePrototypeDemoCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return GeneratePrototypeDemoResult.Failure(
                GeneratePrototypeDemoStatus.ProjectNotFound,
                $"No project found with ID '{request.ProjectId}'.");
        }

        var builder = new StringBuilder();
        await foreach (var chunk in _generationService.GenerateAsync(request.ProjectId, project.Name, cancellationToken))
        {
            builder.Append(chunk);
        }

        return GeneratePrototypeDemoResult.Succeeded(builder.ToString());
    }
}
