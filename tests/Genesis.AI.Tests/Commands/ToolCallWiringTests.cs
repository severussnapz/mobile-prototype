using System.Reflection;
using Genesis.AI.Infrastructure.Services;
using Xunit;

namespace Genesis.AI.Tests.Commands;

/// <summary>
/// Enforcement test: every tool constant in PipelineToolDefinitions must have
/// a corresponding case in ConversationStreamController.ExecuteToolCallAsync.
/// If this test fails, a tool was registered but not wired — it will log
/// "Unknown tool call" at runtime and silently do nothing.
/// </summary>
public class ToolCallWiringTests
{
    [Fact]
    public void AllPipelineToolConstants_HaveCorrespondingCaseInExecuteToolCallAsync()
    {
        // Get all public const string fields from PipelineToolDefinitions
        var toolConstants = typeof(PipelineToolDefinitions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(toolConstants);

        // Read the controller source file
        var controllerPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "..", "..", "..", "..", "..",
            "src", "Genesis.AI.Api", "Features", "Conversations",
            "ConversationStreamController.cs");

        var fullPath = Path.GetFullPath(controllerPath);
        Assert.True(File.Exists(fullPath),
            $"ConversationStreamController.cs not found at: {fullPath}");

        var controllerSource = File.ReadAllText(fullPath);

        // Get the constant name → value mapping
        var constantsByValue = typeof(PipelineToolDefinitions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToDictionary(
                f => (string)f.GetRawConstantValue()!,
                f => f.Name);

        // These tools are sub-operations dispatched internally by apply_to_scope.
        // They are not direct tool calls handled in the ExecuteToolCallAsync switch.
        var intentionallyUnwired = new HashSet<string>
        {
            PipelineToolDefinitions.EditArtefactByGraphNode,
            PipelineToolDefinitions.SetNodeAttribute,
            PipelineToolDefinitions.SetNodeText,
            PipelineToolDefinitions.AddNodeClass,
            PipelineToolDefinitions.RemoveNodeClass,
            PipelineToolDefinitions.InsertAdjacentHtml,
            PipelineToolDefinitions.RemoveElement,
        };

        var unwiredTools = new List<string>();
        foreach (var toolConstant in toolConstants.Where(t => !intentionallyUnwired.Contains(t)))
        {
            var constantName = constantsByValue[toolConstant];
            // Check either the constant reference (PipelineToolDefinitions.SaveArtefact)
            // or the string literal appears in the controller
            var hasConstantRef = controllerSource.Contains(
                $"PipelineToolDefinitions.{constantName}",
                StringComparison.OrdinalIgnoreCase);
            var hasStringLiteral = controllerSource.Contains(
                $"\"{toolConstant}\"",
                StringComparison.OrdinalIgnoreCase);

            if (!hasConstantRef && !hasStringLiteral)
            {
                unwiredTools.Add(toolConstant);
            }
        }

        Assert.True(unwiredTools.Count == 0,
            $"The following tools are registered in PipelineToolDefinitions but have no " +
            $"case in ConversationStreamController.ExecuteToolCallAsync:\n" +
            string.Join("\n", unwiredTools.Select(t => $"  - {t}")));
    }

    [Fact]
    public void ApplyToScope_ControllerCase_TriggersAssemblyAfterSuccessfulMutations()
    {
        // Regression guard: apply_to_scope was completing mutations but not calling
        // AssemblePrototypeAsync — index.html was never updated after bulk edits.
        var controllerSource = File.ReadAllText(
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "..", "..", "..", "..", "..",
                "src", "Genesis.AI.Api", "Features", "Conversations",
                "ConversationStreamController.cs"));

        var applyToScopeIndex = controllerSource.IndexOf(
            "case PipelineToolDefinitions.ApplyToScope:",
            StringComparison.Ordinal);

        Assert.True(applyToScopeIndex >= 0, "apply_to_scope case not found in controller");

        var nextCaseIndex = controllerSource.IndexOf(
            "case PipelineToolDefinitions.",
            applyToScopeIndex + 1,
            StringComparison.Ordinal);

        var applyToScopeBlock = nextCaseIndex > 0
            ? controllerSource[applyToScopeIndex..nextCaseIndex]
            : controllerSource[applyToScopeIndex..];

        Assert.Contains(
            "AssemblePrototypeAsync",
            applyToScopeBlock,
            StringComparison.Ordinal);
    }
}
