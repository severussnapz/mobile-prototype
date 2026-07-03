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
    [Fact]
    public void SearchInArtefact_WhenCalledOnPrototypeIndexHtml_ContainsRedirectToFragmentSearch()
    {
        // Plan 3f: search_in_artefact on prototype/index.html must redirect to DOM fragment search
        // so node_ids returned contain real fragment paths usable by set_node_attribute and apply_to_scope.
        // Agent must never receive node_ids pointing to prototype/index.html.
        var controllerSource = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "src", "Genesis.AI.Api",
                "Features", "Conversations", "ConversationStreamController.cs"));

        Assert.True(
            controllerSource.Contains("PrototypeHtmlArtefactPath", StringComparison.Ordinal) &&
            controllerSource.Contains("_prototypeDomSearchService", StringComparison.Ordinal) &&
            controllerSource.Contains("SearchInArtefact", StringComparison.Ordinal),
            "search_in_artefact handler must redirect prototype/index.html searches to DOM fragment search service.");
    }

    [Fact]
    public void SearchInArtefact_OnPrototypeIndexHtml_RedirectsToFragmentDomSearch()
    {
        // Plan 3f: when search_in_artefact is called with an HTML prototype artefact,
        // the handler must delegate routing to PrototypeSearchRouter and serve the result from
        // PrototypeDomSearchService, which searches actual fragments. This ensures node_ids
        // returned contain real fragment paths usable by mutations. The index.html mapping itself
        // is covered directly by PrototypeSearchRouterTests.
        var controllerSource = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "src", "Genesis.AI.Api",
                "Features", "Conversations", "ConversationStreamController.cs"));

        var searchInArtefactIndex = controllerSource.IndexOf(
            "case PipelineToolDefinitions.SearchInArtefact:", StringComparison.Ordinal);
        var nextCaseIndex = controllerSource.IndexOf("case PipelineToolDefinitions.", 
            searchInArtefactIndex + 1, StringComparison.Ordinal);
        var handlerBody = controllerSource[searchInArtefactIndex..nextCaseIndex];

        Assert.True(
            handlerBody.Contains("PrototypeSearchRouter.ShouldRouteToDomSearch", StringComparison.Ordinal) &&
            handlerBody.Contains("_prototypeDomSearchService", StringComparison.Ordinal),
            "search_in_artefact must delegate routing to PrototypeSearchRouter and serve HTML prototype searches from DOM fragment search.");
    }

    [Fact]
    public void SearchInArtefact_OneMatch_ReturnsReadyApplyToScopeCall()
    {
        // Plan 3f: when search_in_artefact returns exactly one match,
        // the response must include a ready-to-use apply_to_scope call with confirmed selector and scope.
        var controllerSource = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "src", "Genesis.AI.Api",
                "Features", "Conversations", "ConversationStreamController.cs"));

        var searchInArtefactIndex = controllerSource.IndexOf(
            "case PipelineToolDefinitions.SearchInArtefact:", StringComparison.Ordinal);
        var nextCaseIndex = controllerSource.IndexOf("case PipelineToolDefinitions.",
            searchInArtefactIndex + 1, StringComparison.Ordinal);
        var handlerBody = controllerSource[searchInArtefactIndex..nextCaseIndex];

        Assert.True(
            handlerBody.Contains("apply_to_scope", StringComparison.Ordinal),
            "search_in_artefact on one match must return a ready-to-use apply_to_scope call.");
    }

    [Fact]
    public void GetArtefact_ReadBudgetGuard_IsBypassedInPrototypeSingleFileMode()
    {
        // Single-file prototype mode reads requirements/* directly before save_artefact.
        // The non-prototype read-budget guard must be disabled when prototypeSingleFile is true.
        var controllerSource = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "src", "Genesis.AI.Api",
                "Features", "Conversations", "ConversationStreamController.cs"));

        Assert.Contains(
            "if (!isPrototypePath && !prototypeSingleFile)",
            controllerSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SaveArtefact_Pipeline02ContractGuard_IsBypassedInPrototypeSingleFileMode()
    {
        // Single-file prototype mode should not run the fragment-pipeline save contract gate.
        var controllerSource = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "src", "Genesis.AI.Api",
                "Features", "Conversations", "ConversationStreamController.cs"));

        Assert.Contains(
            "if (stageType == StageType.Prototype && !prototypeSingleFile)",
            controllerSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void EditArtefact_SingleFileMode_AllowsPrototypeIndexWhenPrototypeDemoPathWasRead()
    {
        // Single-file mode compatibility: if get_artefact read prototype-demo/index.html,
        // edit_artefact on prototype/index.html should satisfy FILE_NOT_READ via effectiveReadPath.
        var controllerSource = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "src", "Genesis.AI.Api",
                "Features", "Conversations", "ConversationStreamController.cs"));

        Assert.Contains(
            "var effectiveReadPath = prototypeSingleFile",
            controllerSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "filesReadThisRequest.Contains(\"prototype-demo/index.html\")",
            controllerSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "if (!filesReadThisRequest.Contains(effectiveReadPath))",
            controllerSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SearchInArtefact_NoMatch_ReturnsAskUserForHtml()
    {
        // Plan 3f: when search_in_artefact finds no elements,
        // the response must tell the agent to ask the user to paste HTML from browser inspector.
        var controllerSource = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "src", "Genesis.AI.Api",
                "Features", "Conversations", "ConversationStreamController.cs"));

        var searchInArtefactIndex = controllerSource.IndexOf(
            "case PipelineToolDefinitions.SearchInArtefact:", StringComparison.Ordinal);
        var nextCaseIndex = controllerSource.IndexOf("case PipelineToolDefinitions.",
            searchInArtefactIndex + 1, StringComparison.Ordinal);
        var handlerBody = controllerSource[searchInArtefactIndex..nextCaseIndex];

        Assert.True(
            handlerBody.Contains("paste", StringComparison.OrdinalIgnoreCase) ||
            handlerBody.Contains("browser inspector", StringComparison.OrdinalIgnoreCase),
            "search_in_artefact on no match must tell agent to ask user to paste HTML.");
    }

    [Fact]
    public void SearchInArtefact_AmbiguousMatch_ReturnsCandidatesAndAskUser()
    {
        // Plan 3f Fix 2: when search_in_artefact returns multiple matches, the multi-match
        // result builder lists the candidates and only asks the user to confirm/paste HTML
        // when no confirmed selector can be derived (matches span fragments, or no shared class).
        // When all matches share one fragment and one selector, it returns a ready apply_to_scope
        // call instead — so the ask-user path must be guarded by the confirmed-selector check.
        var controllerSource = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "src", "Genesis.AI.Api",
                "Features", "Conversations", "ConversationStreamController.cs"));

        var builderIndex = controllerSource.IndexOf(
            "internal static string BuildDomSearchMultiMatchResult", StringComparison.Ordinal);
        Assert.True(builderIndex >= 0,
            "multi-match result must be built by BuildDomSearchMultiMatchResult.");
        var builderBody = controllerSource[builderIndex..];

        Assert.True(
            builderBody.Contains("ambiguous", StringComparison.OrdinalIgnoreCase) ||
            builderBody.Contains("multiple", StringComparison.OrdinalIgnoreCase),
            "multi-match builder must list candidates for the ambiguous matches.");
        Assert.True(
            builderBody.Contains("confirmedSelector is not null", StringComparison.Ordinal),
            "multi-match builder must gate the ask-user path behind a confirmed-selector check.");
        Assert.True(
            builderBody.Contains("apply_to_scope", StringComparison.Ordinal),
            "multi-match builder must offer a ready apply_to_scope call when a selector is confirmed.");
    }

    [Fact]
    public void ApplyToScope_PartialFailure_ReturnsEscalationSignal()
    {
        // Plan 3f: when apply_to_scope has partial failures,
        // the response must signal the agent to escalate to save_artefact.
        var controllerSource = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "src", "Genesis.AI.Api",
                "Features", "Conversations", "ConversationStreamController.cs"));

        var applyToScopeIndex = controllerSource.IndexOf(
            "case PipelineToolDefinitions.ApplyToScope:", StringComparison.Ordinal);
        var nextCaseIndex = controllerSource.IndexOf("case PipelineToolDefinitions.",
            applyToScopeIndex + 1, StringComparison.Ordinal);
        var handlerBody = controllerSource[applyToScopeIndex..nextCaseIndex];

        Assert.True(
            handlerBody.Contains("save_artefact", StringComparison.OrdinalIgnoreCase),
            "apply_to_scope partial failure must signal agent to escalate to save_artefact.");
    }

    [Fact]
    public void SearchInArtefact_WhenCalledMoreThanThreeTimesOnSameFileWithoutMutation_ReturnsHardStop()
    {
        // Plan 3f enforcement: after 3 search_in_artefact calls on the same file without
        // a mutation tool call in between, the API must return a hard stop forcing the agent
        // to act. This prevents context window exhaustion from repeated searching loops.
        var controllerSource = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "src", "Genesis.AI.Api",
                "Features", "Conversations", "ConversationStreamController.cs"));

        var searchIndex = controllerSource.IndexOf(
            "case PipelineToolDefinitions.SearchInArtefact:", StringComparison.Ordinal);
        var nextCase = controllerSource.IndexOf(
            "case PipelineToolDefinitions.", searchIndex + 1, StringComparison.Ordinal);
        var handlerBody = controllerSource[searchIndex..nextCase];

        Assert.True(
            handlerBody.Contains("searchCount", StringComparison.OrdinalIgnoreCase) ||
            handlerBody.Contains("SearchCount", StringComparison.OrdinalIgnoreCase) ||
            handlerBody.Contains("search_count", StringComparison.OrdinalIgnoreCase),
            "search_in_artefact handler must track per-file search count and hard-stop after 3 searches without a mutation.");
    }

    [Fact]
    public void ApplyToScope_WhenInsertAdjacentHtmlWithGenerateFromContext_RejectsWithActionableError()
    {
        // generate_from_context generates text values (e.g. aria-labels) — it cannot generate
        // HTML to insert. Using it with insert_adjacent_html is always wrong and the API must
        // reject it with a clear error rather than silently returning 0 mutations.
        var controllerSource = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "src", "Genesis.AI.Api",
                "Features", "Conversations", "ConversationStreamController.cs"));

        var applyToScopeIndex = controllerSource.IndexOf(
            "case PipelineToolDefinitions.ApplyToScope:", StringComparison.Ordinal);
        var nextCaseIndex = controllerSource.IndexOf(
            "case PipelineToolDefinitions.", applyToScopeIndex + 1, StringComparison.Ordinal);
        var handlerBody = controllerSource[applyToScopeIndex..nextCaseIndex];

        Assert.True(
            handlerBody.Contains("insert_adjacent_html", StringComparison.Ordinal) &&
            handlerBody.Contains("generate_from_context", StringComparison.Ordinal),
            "apply_to_scope must reject insert_adjacent_html + generate_from_context combination.");
    }

}