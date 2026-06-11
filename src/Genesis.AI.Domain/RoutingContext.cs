using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain;

/// <summary>
/// Lightweight routing context resolved at the start of each streaming turn.
/// Carries the stage type, current phase, and whether this is the first user
/// message in the conversation — used to select system prompt content and
/// active skill blocks.
/// </summary>
public sealed record RoutingContext(
    StageType StageType,
    int CurrentPhase,
    bool IsFirstMessage);
