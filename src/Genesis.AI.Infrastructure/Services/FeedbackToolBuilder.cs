using System.Text.Json;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

internal static class FeedbackToolBuilder
{
    internal static AiToolDefinition BuildProposeRequirementChangeTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.ProposeRequirementChange,
            Description:
                "Propose a change to a requirement when you identify a gap, clarification need, or contradiction.\n\n" +
                "WHEN TO CALL:\n" +
                "- GAP: the requirement is missing acceptance criteria needed for the current pipeline stage\n" +
                "- CLARIFICATION: an existing AC is ambiguous or needs refinement\n" +
                "- CONTRADICTION: two ACs conflict with each other or with another REQ\n\n" +
                "RULES:\n" +
                "- Do NOT call edit_artefact directly on REQ files — always use this tool instead\n" +
                "- proposed_ac_text is required for GAP and CLARIFICATION; omit for CONTRADICTION\n" +
                "- For CONTRADICTION: describe both conflicting items in the rationale; do not propose resolution text\n" +
                "- Impact classification is set by the human in the UI — do not include impact fields\n" +
                "- After calling this tool, continue your current pipeline work — do not wait\n\n" +
                "The change is held for human approval. It is NOT applied until approved.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "req_id": {
                        "type": "string",
                        "description": "The requirement ID to propose a change for, e.g. 'REQ-001'"
                    },
                    "change_type": {
                        "type": "string",
                        "enum": ["gap", "clarification", "contradiction"],
                        "description": "Type of change: gap=missing AC, clarification=ambiguous AC, contradiction=conflicting ACs"
                    },
                    "proposed_ac_text": {
                        "type": "string",
                        "description": "The proposed acceptance criteria text to add. Required for gap and clarification. Omit for contradiction. Start with '- [ ]' format."
                    },
                    "rationale": {
                        "type": "string",
                        "description": "Why this change is needed. For contradiction: describe both conflicting items verbatim."
                    },
                    "clinical_safety_impact": {
                        "type": "string",
                        "enum": ["none", "possible", "definite"],
                        "description": "Clinical safety domain impact assessment. Use possible if there is any patient safety consideration, definite if DCB0129 hazard applies."
                    },
                    "ig_impact": {
                        "type": "string",
                        "enum": ["none", "possible", "definite"],
                        "description": "Information governance domain impact. Use possible if UK GDPR/DSPT may apply, definite if Article 9 special category data or consent is involved."
                    },
                    "security_impact": {
                        "type": "string",
                        "enum": ["none", "possible", "definite"],
                        "description": "Security domain impact. Use possible if authentication/authorisation or data access controls may be affected, definite if a security control is missing."
                    }
                },
                "required": ["req_id", "change_type", "rationale", "clinical_safety_impact", "ig_impact", "security_impact"]
            }
            """));
    }
}
