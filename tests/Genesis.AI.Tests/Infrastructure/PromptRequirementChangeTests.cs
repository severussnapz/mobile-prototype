using System.Reflection;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class PromptRequirementChangeTests
{
    private static readonly string PromptsPath = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "..", "..", "..", "..", "..",
        "src", "Genesis.AI.Infrastructure", "Prompts");

    private static readonly string[] PipelinePromptFiles =
    [
        "Pipeline01RequirementsDiscovery.md",
        "Pipeline02Prototype.md",
        "Pipeline03Architecture.md",
        "Pipeline04Design.md",
        "Pipeline05Pxd.md",
        "Pipeline06ClinicalSafety.md",
        "Pipeline07InformationGovernance.md",
        "Pipeline08Security.md"
    ];

    [Theory]
    [MemberData(nameof(GetPipelinePromptFiles))]
    public void PipelinePrompt_ContainsProposeRequirementChangeTool(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(PromptsPath, fileName));
        Assert.True(File.Exists(path), $"Prompt file not found: {path}");

        var content = File.ReadAllText(path);
        Assert.Contains("propose_requirement_change", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(GetPipelinePromptFiles))]
    public void PipelinePrompt_ContainsRequirementChangeProtocolSection(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(PromptsPath, fileName));
        var content = File.ReadAllText(path);
        Assert.Contains("Requirement Change Protocol", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(GetPipelinePromptFiles))]
    public void PipelinePrompt_ContainsDoNotEditReqDirectly(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(PromptsPath, fileName));
        var content = File.ReadAllText(path);
        Assert.Contains("edit_artefact", content, StringComparison.OrdinalIgnoreCase);
    }

    public static TheoryData<string> GetPipelinePromptFiles()
    {
        var data = new TheoryData<string>();
        foreach (var file in PipelinePromptFiles)
        {
            data.Add(file);
        }
        return data;
    }
}
