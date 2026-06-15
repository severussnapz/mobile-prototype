using System.Text.Json;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

internal static class ParkingLotToolBuilder
{
    internal static AiToolDefinition BuildAddParkingLotItemTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.AddParkingLotItem,
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
            """));
    }

    internal static AiToolDefinition BuildResolveParkingLotItemTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.ResolveParkingLotItem,
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
            """));
    }
}
