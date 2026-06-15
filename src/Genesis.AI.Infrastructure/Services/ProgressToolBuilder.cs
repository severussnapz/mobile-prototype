using System.Text.Json;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

internal static class ProgressToolBuilder
{
    internal static AiToolDefinition BuildUpdateProgressTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.UpdateProgress,
            Description: """Update session progress metrics. Call this after each question-answer exchange. Do NOT output progress numbers in your chat text — use this tool instead. You can call this alongside other tools (e.g. add_parking_lot_item) in the same turn.""",
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
            """));
    }

    internal static AiToolDefinition BuildSetOrchestrationModeTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.SetOrchestrationMode,
            Description: """Explicitly switch the orchestration mode for this conversation. Use ONLY to enter 'cross_check' mode after completing the forward sweep in P6/P7/P8. DO NOT call this during the forward sweep — cross_check mode must never be inferred from turn counts, requirement counts, or queue state. Only one cross-check conversation should exist per stage run.""",
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
            """));
    }
}
