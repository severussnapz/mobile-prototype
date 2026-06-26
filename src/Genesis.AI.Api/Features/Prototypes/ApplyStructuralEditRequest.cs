namespace Genesis.AI.Api.Features.Prototypes;

public sealed class ApplyStructuralEditRequest
{
    public string Operation { get; set; } = string.Empty;
    public string? FragmentPath { get; set; }
    public List<string>? OrderedFragmentPaths { get; set; }
    public bool? Hidden { get; set; }
}
