using System.Text.Json;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Defines the tools available to the AI during pipeline conversations.
/// These are Claude tool-use definitions that produce structured JSON calls
/// instead of relying on prompt-based markers.
/// </summary>
public static class PipelineToolDefinitions
{
    private static readonly Lazy<IReadOnlyList<AiToolDefinition>> _tools = new(BuildTools);

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

    private static IReadOnlyList<AiToolDefinition> BuildTools()
    {
        return
        [
            new AiToolDefinition(
                Name: SaveArtefact,
                Description: "Save a file artefact (e.g. manifest.md, requirements/REQ-001.md) to the project's artefact store. " +
                             "Call this whenever you produce a complete file output. If the same file_path is saved again, it creates a new version. " +
                             "Write your full response text first, then call this tool — you will not get another turn.",
                InputSchema: JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "file_path": {
                            "type": "string",
                            "description": "The relative file path for the artefact (e.g. 'manifest.md' or 'requirements/REQ-001_patient_search.md'). Use forward slashes."
                        },
                        "content": {
                            "type": "string",
                            "description": "The full content of the file to save, including all markdown formatting."
                        }
                    },
                    "required": ["file_path", "content"]
                }
                """)),

            new AiToolDefinition(
                Name: AdvancePhase,
                Description: "Signal that you are transitioning to the next phase of the interview. " +
                             "Call this when you complete a phase and move to the next one. " +
                             "Include your transition statement AND your first question for the new phase as text, " +
                             "then call this tool to record the phase change.",
                InputSchema: JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "phase_number": {
                            "type": "integer",
                            "description": "The phase number you are transitioning TO (e.g. 3 means entering phase 3)."
                        },
                        "phase_name": {
                            "type": "string",
                            "description": "The snake_case name of the phase (e.g. 'users_and_personas', 'core_workflow', 'verify_and_complete_requirement_files')."
                        }
                    },
                    "required": ["phase_number", "phase_name"]
                }
                """)),

            new AiToolDefinition(
                Name: AddParkingLotItem,
                Description: "Add an item to the parking lot — topics, decisions, or implementation details to revisit later. " +
                             "Use this EVERY TIME the user mentions an integration point, technical decision, configuration need, " +
                             "or any detail that will need further specification (e.g. 'Teams channel', 'email capture', 'webhook URL'). " +
                             "Call this alongside your response — you can call multiple tools in one turn.",
                InputSchema: JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "priority": {
                            "type": "string",
                            "enum": ["critical", "high", "medium"],
                            "description": "Priority level: 'critical' = blocks progress, 'high' = resolve before output, 'medium' = nice to have."
                        },
                        "content": {
                            "type": "string",
                            "description": "Brief description of the item to revisit (e.g. 'Confirm DPIA completion with IG team')."
                        }
                    },
                    "required": ["priority", "content"]
                }
                """)),

            new AiToolDefinition(
                Name: ResolveParkingLotItem,
                Description: "Mark an existing parking lot item as resolved. Use this when a previously parked topic has been addressed " +
                             "during the conversation and no longer needs follow-up. The item ID is visible in the session state parking lot list.",
                InputSchema: JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "item_id": {
                            "type": "string",
                            "description": "The UUID of the parking lot item to resolve (from the parking lot shown in session state)."
                        }
                    },
                    "required": ["item_id"]
                }
                """)),

            new AiToolDefinition(
                Name: UpdateProgress,
                Description: "Update session progress metrics. Call this after each question-answer exchange. " +
                             "Do NOT output progress numbers in your chat text — use this tool instead. " +
                             "You can call this alongside other tools (e.g. add_parking_lot_item) in the same turn.",
                InputSchema: JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "questions_asked": {
                            "type": "integer",
                            "description": "Total number of questions asked so far in this conversation."
                        },
                        "estimated_total": {
                            "type": "integer",
                            "description": "Your current estimate of total questions needed to complete this stage (can change as you learn more)."
                        },
                        "requirements_captured": {
                            "type": "integer",
                            "description": "Number of distinct requirements identified so far (requirements stages only, 0 otherwise)."
                        }
                    },
                    "required": ["questions_asked", "estimated_total"]
                }
                """)),

            new AiToolDefinition(
                Name: ListArtefacts,
                Description: "List all artefacts (files) that have been saved to the project. " +
                             "Returns file paths, versions, and timestamps. Use this to discover what files exist " +
                             "before requesting their content with get_artefact. " +
                             "Call this when you need to understand what prior work has been produced.",
                InputSchema: JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {},
                    "required": []
                }
                """)),

            new AiToolDefinition(
                Name: GetArtefact,
                Description: "Retrieve the full content of a specific artefact file. " +
                             "Use list_artefacts first to see available files, then call this with the file_path. " +
                             "Call this when you need to read prior stage outputs (requirements, architecture decisions, etc.) " +
                             "to inform your current work.",
                InputSchema: JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "file_path": {
                            "type": "string",
                            "description": "The exact file path of the artefact to retrieve (as shown by list_artefacts)."
                        }
                    },
                    "required": ["file_path"]
                }
                """)),

            new AiToolDefinition(
                Name: GetGuardrailDetails,
                Description: "Retrieve the full content of a guardrail/skill document by name. " +
                             "Use this to load detailed rules and patterns when you need domain-specific guidance.",
                InputSchema: JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "skill_name": {
                            "type": "string",
                            "description": "The skill name to retrieve (e.g. 'emis-x-api-auth', 'emis-x-api-security')."
                        }
                    },
                    "required": ["skill_name"]
                }
                """)),

            new AiToolDefinition(
                Name: SetOrchestrationMode,
                Description: "Explicitly switch the orchestration mode for this conversation. " +
                             "Use ONLY to enter 'cross_check' mode after completing the forward sweep in P6/P7/P8. " +
                             "DO NOT call this during the forward sweep — cross_check mode must never be inferred from " +
                             "turn counts, requirement counts, or queue state. Only one cross-check conversation " +
                             "should exist per stage run.",
                InputSchema: JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "mode": {
                            "type": "string",
                            "enum": ["cross_check"],
                            "description": "The orchestration mode to enter. Only 'cross_check' is a valid transition target."
                        },
                        "justification": {
                            "type": "string",
                            "description": "Explanation of why the cross-check mode is being entered now (e.g. 'Forward sweep complete for all N requirements. Entering cross-check to verify HAZ-ID monotonicity.')."
                        }
                    },
                    "required": ["mode", "justification"]
                }
                """))
        ,

            new AiToolDefinition(
                Name: AdvanceRequirement,
                Description: "Signal that you have fully completed the current requirement conversation. " +
                             "Call this ONLY after you have saved the requirement artefact (requirements/REQ-xxx.md) " +
                             "for this requirement in the current session. " +
                             "The API will reject this call if no requirement artefact has been persisted yet — " +
                             "save the artefact first, then call this tool. " +
                             "Do not call this tool to advance between interview phases — use advance_phase for that.",
                InputSchema: JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "requirement_id": {
                            "type": "string",
                            "description": "The requirement identifier being completed (e.g. 'REQ-001'). Must match the requirement assigned to this conversation."
                        },
                        "summary": {
                            "type": "string",
                            "description": "Brief one-line summary of what was captured for this requirement."
                        }
                    },
                    "required": ["requirement_id"]
                }
                """))
        ];
    }
}
