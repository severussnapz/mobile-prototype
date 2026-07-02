using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Configuration;

public sealed class PrototypeDemoOptions : IPrototypeDemoSettings
{
    public const string SectionName = "PrototypeDemo";

    public int GenerationTimeoutMinutes { get; set; } = 3;

    public bool UseStubGeneration { get; set; }

    public TimeSpan GenerationTimeout => TimeSpan.FromMinutes(GenerationTimeoutMinutes);
}