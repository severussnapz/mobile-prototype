using System.Text.Json;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

internal static class SkillToolBuilder
{
    internal static AiToolDefinition BuildGetGuardrailDetailsTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.GetGuardrailDetails,
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
            """));
    }
}
