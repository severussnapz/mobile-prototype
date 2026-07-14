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
    public void GetTools_PrototypeWithDomModeEnabled_ExcludesGraphNodeToolAndIncludesScopeTools()
    {
        var options = new TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.Prototype);

        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.EditArtefactByGraphNode);
        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.InsertAdjacentHtml);
        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.RemoveElement);
        Assert.Contains(tools, tool => tool.Name == PipelineToolDefinitions.ApplyToScope);
    }

    [Fact]
    public void GetTools_PrototypeWithDomModeEnabled_BuildsValidApplyToScopeInputSchema()
    {
        var options = new TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.Prototype);
        var scopeTool = Assert.Single(tools, tool => tool.Name == PipelineToolDefinitions.ApplyToScope);

        var schemaRoot = scopeTool.InputSchema.RootElement;
        Assert.Equal("object", schemaRoot.GetProperty("type").GetString());

        var properties = schemaRoot.GetProperty("properties");
        Assert.Equal("string", properties.GetProperty("scope").GetProperty("type").GetString());
        Assert.Equal("string", properties.GetProperty("selector").GetProperty("type").GetString());
        Assert.Equal("string", properties.GetProperty("operation").GetProperty("type").GetString());
        Assert.Equal("string", properties.GetProperty("strategy").GetProperty("type").GetString());

        var required = schemaRoot.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToArray();
        Assert.Contains("scope", required);
        Assert.Contains("selector", required);
        Assert.Contains("operation", required);
        Assert.Contains("strategy", required);
    }

    [Fact]
    public void GetTools_PrototypeWithDomModeDisabled_ExcludesGraphNodeToolAndDomStructureTools()
    {
        var options = new TokenOptimisationOptions
        {
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = false,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.Prototype);

        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.EditArtefactByGraphNode);
        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.InsertAdjacentHtml);
        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.RemoveElement);
    }

    [Fact]
    public void ApplyToScope_ToolDescription_ContainsStrategyInstructions()
    {
        var artefactToolBuilderType = typeof(PipelineToolDefinitions).Assembly
            .GetType("Genesis.AI.Infrastructure.Services.PrototypeDomToolBuilder");
        Assert.NotNull(artefactToolBuilderType);

        var buildMethod = artefactToolBuilderType!.GetMethod(
            "BuildApplyToScopeTool",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(buildMethod);

        var tool = buildMethod!.Invoke(null, null) as AiToolDefinition;
        Assert.NotNull(tool);

        Assert.Contains("literal", tool!.Description, StringComparison.Ordinal);
        Assert.Contains("derive_from_text_content", tool.Description, StringComparison.Ordinal);
        Assert.Contains("generate_from_context", tool.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void GetTools_PrototypeSingleFileEnabled_ReturnsExactlySingleFileToolSet()
    {
        var options = new TokenOptimisationOptions
        {
            PrototypeSingleFileEnabled = true,
            EditArtefactEnabled = true,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.Prototype);
        var names = tools.Select(tool => tool.Name).ToArray();

        string[] expected =
        [
            PipelineToolDefinitions.SaveArtefact,
            PipelineToolDefinitions.EditArtefact,
            PipelineToolDefinitions.GetArtefact,
            PipelineToolDefinitions.ListArtefacts,
            PipelineToolDefinitions.AddParkingLotItem,
            PipelineToolDefinitions.ResolveParkingLotItem,
            PipelineToolDefinitions.ProposeRequirementChange,
            PipelineToolDefinitions.UpdateProgress
        ];

        Assert.Equal(expected.OrderBy(name => name), names.OrderBy(name => name));
    }

    [Fact]
    public void GetTools_PrototypeSingleFileEnabled_ExcludesFragmentAndWindowingTools()
    {
        var options = new TokenOptimisationOptions
        {
            PrototypeSingleFileEnabled = true,
            EditArtefactEnabled = true,
            PrototypeDomModeEnabled = true,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.Prototype);

        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.ApplyToScope);
        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.AdvanceRequirement);
        Assert.DoesNotContain(tools, tool => tool.Name == PipelineToolDefinitions.SetOrchestrationMode);
    }

    [Fact]
    public void GetTools_PrototypeSingleFileDisabled_KeepsWindowingTools()
    {
        // Regression: with the flag off the Prototype stage keeps the existing base tool set.
        var options = new TokenOptimisationOptions
        {
            PrototypeSingleFileEnabled = false,
            EditArtefactEnabled = true,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.Prototype);

        Assert.Contains(tools, tool => tool.Name == PipelineToolDefinitions.AdvanceRequirement);
        Assert.Contains(tools, tool => tool.Name == PipelineToolDefinitions.SetOrchestrationMode);
    }

    [Fact]
    public void GetTools_PrototypeSingleFileEnabledOnNonPrototypeStage_DoesNotApplySingleFileSet()
    {
        // The single-file set is scoped to the Prototype stage only.
        var options = new TokenOptimisationOptions
        {
            PrototypeSingleFileEnabled = true,
            EditArtefactEnabled = true,
            ActiveSkillInjectionEnabled = false
        };

        var tools = PipelineToolDefinitions.GetTools(options, StageType.Design);

        Assert.Contains(tools, tool => tool.Name == PipelineToolDefinitions.AdvanceRequirement);
    }
}
