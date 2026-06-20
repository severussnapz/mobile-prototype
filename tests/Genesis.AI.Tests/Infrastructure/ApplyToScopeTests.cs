using Genesis.AI.Infrastructure.Services;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class ApplyToScopeTests
{
    [Fact]
    public void PipelineToolDefinitions_HasApplyToScopeConstant()
    {
        Assert.Equal("apply_to_scope", PipelineToolDefinitions.ApplyToScope);
    }

    [Fact]
    public void PipelineToolDefinitions_DoesNotHaveListElementsConstant()
    {
        var fields = typeof(PipelineToolDefinitions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => f.Name)
            .ToList();

        Assert.DoesNotContain("ListElements", fields);
    }

    [Fact]
    public void PipelineToolDefinitions_DoesNotHaveApplyBulkAttributesConstant()
    {
        var fields = typeof(PipelineToolDefinitions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => f.Name)
            .ToList();

        Assert.DoesNotContain("ApplyBulkAttributes", fields);
    }

    [Fact]
    public void GetTools_PrototypeWithDomModeEnabled_IncludesApplyToScope()
    {
        var options = new Genesis.AI.Infrastructure.Configuration.TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true
        };

        var tools = PipelineToolDefinitions.GetTools(options, Genesis.AI.Domain.Enums.StageType.Prototype);
        var toolNames = tools.Select(t => t.Name).ToList();

        Assert.Contains("apply_to_scope", toolNames);
    }

    [Fact]
    public void GetTools_PrototypeWithDomModeEnabled_DoesNotIncludeListElements()
    {
        var options = new Genesis.AI.Infrastructure.Configuration.TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true
        };

        var tools = PipelineToolDefinitions.GetTools(options, Genesis.AI.Domain.Enums.StageType.Prototype);
        var toolNames = tools.Select(t => t.Name).ToList();

        Assert.DoesNotContain("list_elements", toolNames);
    }

    [Fact]
    public void GetTools_PrototypeWithDomModeEnabled_DoesNotIncludeApplyBulkAttributes()
    {
        var options = new Genesis.AI.Infrastructure.Configuration.TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true
        };

        var tools = PipelineToolDefinitions.GetTools(options, Genesis.AI.Domain.Enums.StageType.Prototype);
        var toolNames = tools.Select(t => t.Name).ToList();

        Assert.DoesNotContain("apply_bulk_attributes", toolNames);
    }
}
