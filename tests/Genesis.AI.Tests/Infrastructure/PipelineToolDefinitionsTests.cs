using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Configuration;
using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Infrastructure;

public class PipelineToolDefinitionsTests
{
    [Fact]
    public void GetTools_NonPrototypeWithEditEnabled_IncludesEditArtefactAndExcludesGraphNodeTool()
    {
        var options = new TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.RequirementsDiscovery);

        Assert.Contains(tools, tool => tool.Name == PipelineToolDefinitions.EditArtefact);
        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.EditArtefactByGraphNode);
    }

    [Fact]
    public void GetTools_NonPrototypeWithEditEnabled_BuildsValidEditArtefactInputSchema()
    {
        var options = new TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.RequirementsDiscovery);
        var editTool = Assert.Single(tools, tool => tool.Name == PipelineToolDefinitions.EditArtefact);

        var schemaRoot = editTool.InputSchema.RootElement;
        Assert.Equal("object", schemaRoot.GetProperty("type").GetString());

        var properties = schemaRoot.GetProperty("properties");
        Assert.Equal("string", properties.GetProperty("file_path").GetProperty("type").GetString());
        Assert.Equal("string", properties.GetProperty("old_str").GetProperty("type").GetString());
        Assert.Equal("string", properties.GetProperty("new_str").GetProperty("type").GetString());

        var required = schemaRoot.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToArray();
        Assert.Contains("file_path", required);
        Assert.Contains("old_str", required);
        Assert.Contains("new_str", required);
    }

    [Fact]
    public void GetTools_PrototypeWithDomModeEnabled_ExcludesGraphNodeToolAndIncludesDomStructureTools()
    {
        var options = new TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.Prototype);

        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.EditArtefactByGraphNode);
        Assert.Contains(tools, tool => tool.Name == PipelineToolDefinitions.InsertAdjacentHtml);
        Assert.Contains(tools, tool => tool.Name == PipelineToolDefinitions.RemoveElement);
        Assert.Contains(tools, tool => tool.Name == PipelineToolDefinitions.ApplyBulkAttributes);
    }

    [Fact]
    public void GetTools_PrototypeWithDomModeEnabled_BuildsValidApplyBulkAttributesInputSchema()
    {
        var options = new TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.Prototype);
        var bulkTool = Assert.Single(tools, tool => tool.Name == PipelineToolDefinitions.ApplyBulkAttributes);

        var schemaRoot = bulkTool.InputSchema.RootElement;
        Assert.Equal("object", schemaRoot.GetProperty("type").GetString());

        var properties = schemaRoot.GetProperty("properties");
        Assert.Equal("string", properties.GetProperty("attribute").GetProperty("type").GetString());

        var snippetValuePairs = properties.GetProperty("snippet_value_pairs");
        Assert.Equal("array", snippetValuePairs.GetProperty("type").GetString());

        var itemProperties = snippetValuePairs.GetProperty("items").GetProperty("properties");
        Assert.Equal("string", itemProperties.GetProperty("text_snippet").GetProperty("type").GetString());
        Assert.Equal("string", itemProperties.GetProperty("value").GetProperty("type").GetString());

        var itemRequired = snippetValuePairs.GetProperty("items").GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToArray();
        Assert.Contains("text_snippet", itemRequired);
        Assert.Contains("value", itemRequired);

        var required = schemaRoot.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToArray();
        Assert.Contains("attribute", required);
        Assert.Contains("snippet_value_pairs", required);
    }

    [Fact]
    public void GetTools_PrototypeWithDomModeDisabled_IncludesGraphNodeToolAndExcludesDomStructureTools()
    {
        var options = new TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = false,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.Prototype);

        Assert.Contains(tools, tool => tool.Name == PipelineToolDefinitions.EditArtefactByGraphNode);
        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.InsertAdjacentHtml);
        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.RemoveElement);
    }

    [Fact]
    public void ApplyBulkAttributes_ToolDescription_ContainsWorkflowInstructions()
    {
        var artefactToolBuilderType = typeof(PipelineToolDefinitions).Assembly
            .GetType("Genesis.AI.Infrastructure.Services.ArtefactToolBuilder");
        Assert.NotNull(artefactToolBuilderType);

        var buildMethod = artefactToolBuilderType!.GetMethod(
            "BuildApplyBulkAttributesTool",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(buildMethod);

        var tool = buildMethod!.Invoke(null, null) as AiToolDefinition;
        Assert.NotNull(tool);

        Assert.Contains("apply_bulk_attributes ONCE", tool!.Description, StringComparison.Ordinal);
        Assert.Contains("Do NOT call set_node_attribute", tool.Description, StringComparison.Ordinal);
        Assert.Contains("WORKFLOW", tool.Description, StringComparison.Ordinal);
    }
}
