namespace Genesis.AI.Api.Features.RequirementChanges;

public sealed class ApproveRequirementChangeRequest
{
    public string? ApprovedAcText { get; set; }
    public string ClinicalSafetyImpact { get; set; } = "none";
    public string IgImpact { get; set; } = "none";
    public string SecurityImpact { get; set; } = "none";
}
