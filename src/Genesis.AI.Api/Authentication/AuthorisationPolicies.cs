namespace Genesis.AI.Api.Authentication;

public static class AuthorisationPolicies
{
    public const string ProjectRead = nameof(ProjectRead);
    public const string ProjectWrite = nameof(ProjectWrite);
    public const string ConversationRead = nameof(ConversationRead);
    public const string ConversationWrite = nameof(ConversationWrite);
    public const string ArchitectureConverse = nameof(ArchitectureConverse);
    public const string ProductDesignConverse = nameof(ProductDesignConverse);
    public const string ClinicalSafetyConverse = nameof(ClinicalSafetyConverse);
}
