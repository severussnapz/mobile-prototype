using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class AssemblyVersionProvider : IAssemblyVersionProvider
{
    public string GetVersion()
        => typeof(AssemblyVersionProvider).Assembly
               .GetName().Version?.ToString() ?? "1.0.0.0";
}
