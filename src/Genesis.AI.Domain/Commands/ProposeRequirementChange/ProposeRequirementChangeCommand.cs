using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;

namespace Genesis.AI.Domain.Commands.ProposeRequirementChange;

public sealed record ProposeRequirementChangeCommand(
    Guid ProjectId,
    string ReqId,
    ChangeType ChangeType,
    string RaisingPipeline,
    Guid? RaisingPipelineConversationId,
    string? ProposedAcText,
    string Rationale,
    string CreatedBy,
    ImpactLevel ClinicalSafetyImpact = ImpactLevel.None,
    ImpactLevel IgImpact = ImpactLevel.None,
    ImpactLevel SecurityImpact = ImpactLevel.None);
