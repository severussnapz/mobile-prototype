using MediatR;

namespace Genesis.AI.Domain.Commands.GeneratePrototypeDemo;

/// <summary>
/// Generates a prototype-demo HTML page for a project.
/// </summary>
public sealed record GeneratePrototypeDemoCommand(Guid ProjectId, string UserId)
    : IRequest<GeneratePrototypeDemoResult>;
