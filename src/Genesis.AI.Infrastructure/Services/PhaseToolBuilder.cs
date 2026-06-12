using System.Text.Json;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

internal static class PhaseToolBuilder
{
    internal static AiToolDefinition BuildAdvancePhaseTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.AdvancePhase,
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
            """));
    }

    internal static AiToolDefinition BuildAdvanceRequirementTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.AdvanceRequirement,
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
            """));
    }
}
