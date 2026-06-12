using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Configuration;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Defines the tools available to the AI during pipeline conversations.
/// These are Claude tool-use definitions that produce structured JSON calls
/// instead of relying on prompt-based markers.
/// </summary>
public static class PipelineToolDefinitions
{
    private static readonly Lazy<IReadOnlyList<AiToolDefinition>> _tools =
        new(PipelineToolDefinitionFactory.BuildBaseTools);

    public static IReadOnlyList<AiToolDefinition> All => _tools.Value;

    public const string SaveArtefact = "save_artefact";
    public const string AdvancePhase = "advance_phase";
    public const string AddParkingLotItem = "add_parking_lot_item";
    public const string ResolveParkingLotItem = "resolve_parking_lot_item";
    public const string UpdateProgress = "update_progress";
    public const string GetGuardrailDetails = "get_guardrail_details";
    public const string ListArtefacts = "list_artefacts";
    public const string GetArtefact = "get_artefact";
    public const string SetOrchestrationMode = "set_orchestration_mode";
    public const string AdvanceRequirement = "advance_requirement";
    public const string EditArtefact = "edit_artefact";

    /// <summary>
    /// Returns the tool list conditioned on <paramref name="options"/>.
    /// Includes <c>edit_artefact</c> when <see cref="TokenOptimisationOptions.EditArtefactEnabled"/> is true.
    /// </summary>
    public static IReadOnlyList<AiToolDefinition> GetTools(TokenOptimisationOptions options)
    {
        if (!options.EditArtefactEnabled)
            return All;

        var tools = new List<AiToolDefinition>(All)
        {
            PipelineToolDefinitionFactory.BuildEditArtefactTool()
        };
        return tools.AsReadOnly();
    }
}
