using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services.GitHub;

public sealed class CodeownersGenerator : ICodeownersGenerator
{
    public string Generate() =>
        """
        # Genesis AI — Pipeline Prompt Ownership
        # Team-based ownership. Never individual names.
        # Team membership and role assignments are managed in the EMIS-X Auth / IAM platform.

        src/Genesis.AI.Infrastructure/Prompts/Pipeline06* @emisgroup/clinical-safety-owners
        src/Genesis.AI.Infrastructure/Prompts/Pipeline07* @emisgroup/ig-owners
        src/Genesis.AI.Infrastructure/Prompts/Pipeline08* @emisgroup/security-owners
        """;
}
