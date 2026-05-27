namespace Genesis.AI.Domain.Commands.AdvancePhase;

public record AdvancePhaseResult(bool Found, string? ValidationError, int? Phase = null, string? PhaseName = null);
