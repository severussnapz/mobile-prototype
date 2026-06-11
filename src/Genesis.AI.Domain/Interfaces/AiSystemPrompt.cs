namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Represents a system prompt split into a stable foundation part (cached by Bedrock)
/// and a mutable part (fresh every turn — session state, artefact manifest, etc.).
/// </summary>
/// <param name="StablePart">
/// Content placed before the Bedrock cache point. Should contain the base stage prompt and
/// injected Category A (foundation) artefacts that do not change during a stage run.
/// </param>
/// <param name="MutablePart">
/// Content placed after the Bedrock cache point. Contains session state, artefact manifest
/// and any other context that changes between turns.
/// </param>
public record AiSystemPrompt(string StablePart, string MutablePart)
{
    /// <summary>
    /// Creates an <see cref="AiSystemPrompt"/> with no foundation split — the full prompt
    /// is treated as the stable part and the mutable part is empty.
    /// Used when the foundation-prefix feature flag is disabled.
    /// </summary>
    public static AiSystemPrompt FromFullPrompt(string fullPrompt) =>
        new(StablePart: fullPrompt, MutablePart: string.Empty);
}
