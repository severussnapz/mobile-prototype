namespace Genesis.AI.Api.Features.RequirementChanges;

public sealed class ProposeRequirementChangeRequest
{
    public string ReqId { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string? ProposedAcText { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public Guid? RaisingPipelineConversationId { get; set; }
}
