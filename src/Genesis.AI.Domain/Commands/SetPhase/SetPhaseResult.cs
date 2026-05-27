namespace Genesis.AI.Domain.Commands.SetPhase;

public record SetPhaseResult(bool Found, string? ValidationError, int? Phase = null, string? PhaseName = null);
