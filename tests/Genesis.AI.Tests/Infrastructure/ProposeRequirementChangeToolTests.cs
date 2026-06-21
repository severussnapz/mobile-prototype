using Genesis.AI.Domain.Enums;
using Genesis.AI.Infrastructure.Configuration;
using Genesis.AI.Infrastructure.Services;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class ProposeRequirementChangeToolTests
{
    [Fact]
    public void PipelineToolDefinitions_HasProposeRequirementChangeConstant()
    {
        Assert.Equal("propose_requirement_change", PipelineToolDefinitions.ProposeRequirementChange);
    }

    [Fact]
    public void GetTools_WhenRequirementFeedbackEnabled_IncludesProposeRequirementChangeTool()
    {
        var options = new TokenOptimisationOptions
        {
            RequirementFeedbackEnabled = true
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.RequirementsDiscovery);
        var toolNames = tools.Select(tool => tool.Name).ToList();

        Assert.Contains(PipelineToolDefinitions.ProposeRequirementChange, toolNames);
    }

    [Fact]
    public void GetTools_WhenRequirementFeedbackDisabled_ExcludesProposeRequirementChangeTool()
    {
        var options = new TokenOptimisationOptions
        {
            RequirementFeedbackEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.RequirementsDiscovery);
        var toolNames = tools.Select(tool => tool.Name).ToList();

        Assert.DoesNotContain(PipelineToolDefinitions.ProposeRequirementChange, toolNames);
    }

    [Fact]
    public void ProposeRequirementChangeTool_InputSchema_HasRequiredParameters()
    {
        var options = new TokenOptimisationOptions
        {
            RequirementFeedbackEnabled = true
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.RequirementsDiscovery);
        var tool = tools.FirstOrDefault(t =>
            t.Name == PipelineToolDefinitions.ProposeRequirementChange);

        Assert.NotNull(tool);

        var schemaRoot = tool!.InputSchema.RootElement;
        var properties = schemaRoot.GetProperty("properties");

        Assert.True(properties.TryGetProperty("req_id", out _));
        Assert.True(properties.TryGetProperty("change_type", out _));
        Assert.True(properties.TryGetProperty("rationale", out _));
        Assert.True(properties.TryGetProperty("proposed_ac_text", out _));

        var required = schemaRoot.GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Contains("req_id", required);
        Assert.Contains("change_type", required);
        Assert.Contains("rationale", required);
        Assert.DoesNotContain("proposed_ac_text", required);
    }

    [Fact]
    public void ProposeRequirementChangeTool_ChangeTypeEnum_ContainsAllTypes()
    {
        var options = new TokenOptimisationOptions { RequirementFeedbackEnabled = true };
        var tools = PipelineToolDefinitions.GetTools(options, StageType.RequirementsDiscovery);
        var tool = tools.FirstOrDefault(t =>
            t.Name == PipelineToolDefinitions.ProposeRequirementChange);

        Assert.NotNull(tool);

        var changeTypeEnum = tool!.InputSchema.RootElement
            .GetProperty("properties")
            .GetProperty("change_type")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Contains("gap", changeTypeEnum);
        Assert.Contains("clarification", changeTypeEnum);
        Assert.Contains("contradiction", changeTypeEnum);
    }
}
