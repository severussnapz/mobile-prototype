using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;

namespace Genesis.AI.Domain.Commands.ApproveRequirementChange;

public sealed record ApproveRequirementChangeCommand(
    Guid ProjectId,
    Guid ChangeId,
    string? ApprovedAcText,
    ImpactLevel ClinicalSafetyImpact,
    ImpactLevel IgImpact,
    ImpactLevel SecurityImpact,
    string ApprovedBy);
