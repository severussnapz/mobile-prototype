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
    private readonly IPrototypeDemoSettings _prototypeDemoSettings;

    public GeneratePrototypeDemoCommandHandler(
        IProjectRepository projectRepository,
        IPrototypeDemoGenerationService generationService,
        IPrototypeDemoSettings prototypeDemoSettings)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
        _prototypeDemoSettings = prototypeDemoSettings ?? throw new ArgumentNullException(nameof(prototypeDemoSettings));
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
        var generationTimeout = _prototypeDemoSettings.GenerationTimeout;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(generationTimeout);

        try
        {
            await foreach (var chunk in _generationService.GenerateAsync(request.ProjectId, project.Name, timeoutCts.Token))
            {
                builder.Append(chunk);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            return GeneratePrototypeDemoResult.Failure(
                GeneratePrototypeDemoStatus.TimedOut,
                $"Prototype generation timed out after {generationTimeout.TotalMinutes:0} minutes. Please try again.");
        }
        catch (TimeoutException exception)
        {
            return GeneratePrototypeDemoResult.Failure(
                GeneratePrototypeDemoStatus.TimedOut,
                exception.Message);
        }
        catch (Exception exception)
        {
            return GeneratePrototypeDemoResult.Failure(
                GeneratePrototypeDemoStatus.GenerationFailed,
                $"Prototype generation failed: {exception.Message}");
        }

        return GeneratePrototypeDemoResult.Succeeded(builder.ToString());
    }
}
