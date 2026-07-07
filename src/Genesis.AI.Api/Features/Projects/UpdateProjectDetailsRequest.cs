namespace Genesis.AI.Api.Features.Projects;

public sealed class UpdateProjectDetailsRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? TimeSheetCode { get; init; }
    public string? ComplianceDomain { get; init; }
}
