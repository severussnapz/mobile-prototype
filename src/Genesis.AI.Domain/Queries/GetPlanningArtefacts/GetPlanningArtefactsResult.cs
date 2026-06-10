using Genesis.AI.Domain.Planning;

namespace Genesis.AI.Domain.Queries.GetPlanningArtefacts;

public sealed record GetPlanningArtefactsResult(
    bool Found,
    IReadOnlyList<PlanningArtefactSummary> Artefacts);
