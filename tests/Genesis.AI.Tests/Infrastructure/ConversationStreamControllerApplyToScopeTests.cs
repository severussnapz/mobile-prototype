using Genesis.AI.Infrastructure.Services;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class ConversationStreamControllerApplyToScopeTests
{
    [Fact]
    public void PipelineToolDefinitions_ApplyToScope_IsCorrectToolName()
    {
        Assert.Equal("apply_to_scope", PipelineToolDefinitions.ApplyToScope);
    }

    [Fact]
    public void ApplyToScopeTool_RequiredParameters_ArePresent()
    {
        var options = new Genesis.AI.Infrastructure.Configuration.TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true
        };

        var tools = PipelineToolDefinitions.GetTools(options, Genesis.AI.Domain.Enums.StageType.Prototype);
        var tool = tools.FirstOrDefault(t => t.Name == PipelineToolDefinitions.ApplyToScope);

        Assert.NotNull(tool);

        var required = tool!.InputSchema.RootElement
            .GetProperty("required")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        Assert.Contains("scope", required);
        Assert.Contains("selector", required);
        Assert.Contains("operation", required);
        Assert.Contains("strategy", required);
        Assert.DoesNotContain("value", required); // value is optional (only for literal strategy)
    }

    [Fact]
    public void ApplyToScopeTool_OperationEnum_ContainsAllOperations()
    {
        var options = new Genesis.AI.Infrastructure.Configuration.TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true
        };

        var tools = PipelineToolDefinitions.GetTools(options, Genesis.AI.Domain.Enums.StageType.Prototype);
        var tool = tools.FirstOrDefault(t => t.Name == PipelineToolDefinitions.ApplyToScope);

        Assert.NotNull(tool);

        var operationEnum = tool!.InputSchema.RootElement
            .GetProperty("properties")
            .GetProperty("operation")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        Assert.Contains("set_attribute", operationEnum);
        Assert.Contains("add_class", operationEnum);
        Assert.Contains("remove_class", operationEnum);
        Assert.Contains("set_text", operationEnum);
        Assert.Contains("remove_attribute", operationEnum);
        Assert.Contains("insert_adjacent_html", operationEnum);
    }

    [Fact]
    public void ApplyToScopeTool_StrategyEnum_ContainsAllStrategies()
    {
        var options = new Genesis.AI.Infrastructure.Configuration.TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true
        };

        var tools = PipelineToolDefinitions.GetTools(options, Genesis.AI.Domain.Enums.StageType.Prototype);
        var tool = tools.FirstOrDefault(t => t.Name == PipelineToolDefinitions.ApplyToScope);

        Assert.NotNull(tool);

        var strategyEnum = tool!.InputSchema.RootElement
            .GetProperty("properties")
            .GetProperty("strategy")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        Assert.Contains("literal", strategyEnum);
        Assert.Contains("derive_from_text_content", strategyEnum);
        Assert.Contains("generate_from_context", strategyEnum);
    }
}
