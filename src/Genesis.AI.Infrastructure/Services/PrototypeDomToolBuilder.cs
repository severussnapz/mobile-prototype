using System.Text.Json;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

internal static class PrototypeDomToolBuilder
{
    internal static AiToolDefinition BuildSetNodeAttributeTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.SetNodeAttribute,
            Description: "Set or replace a single attribute on a graph node in a prototype fragment. " +
                         "Use this for targeted changes like title, aria-label, placeholder, hidden, or disabled.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "node_id": {
                        "type": "string",
                        "description": "Stable graph node ID from search_in_artefact results."
                    },
                    "attribute": {
                        "type": "string",
                        "description": "Attribute name to set, e.g. title, aria-label, placeholder."
                    },
                    "value": {
                        "type": "string",
                        "description": "Attribute value to apply."
                    }
                },
                "required": ["node_id", "attribute", "value"]
            }
            """));
    }

    internal static AiToolDefinition BuildSetNodeTextTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.SetNodeText,
            Description: "Set visible text content for a simple leaf graph node. " +
                         "Use this only when the node has no nested child elements.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "node_id": {
                        "type": "string",
                        "description": "Stable graph node ID from search_in_artefact results."
                    },
                    "text": {
                        "type": "string",
                        "description": "New visible text content (plain text, not HTML)."
                    }
                },
                "required": ["node_id", "text"]
            }
            """));
    }

    internal static AiToolDefinition BuildAddNodeClassTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.AddNodeClass,
            Description: "Add a single CSS class to a graph node in a prototype fragment.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "node_id": {
                        "type": "string",
                        "description": "Stable graph node ID from search_in_artefact results."
                    },
                    "class_name": {
                        "type": "string",
                        "description": "Single class name to add."
                    }
                },
                "required": ["node_id", "class_name"]
            }
            """));
    }

    internal static AiToolDefinition BuildRemoveNodeClassTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.RemoveNodeClass,
            Description: "Remove a single CSS class from a graph node in a prototype fragment.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "node_id": {
                        "type": "string",
                        "description": "Stable graph node ID from search_in_artefact results."
                    },
                    "class_name": {
                        "type": "string",
                        "description": "Single class name to remove."
                    }
                },
                "required": ["node_id", "class_name"]
            }
            """));
    }

    internal static AiToolDefinition BuildInsertAdjacentHtmlTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.InsertAdjacentHtml,
            Description: "Insert HTML adjacent to a graph node in a prototype fragment. " +
                         "Use this for structural inserts near an existing node.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "node_id": {
                        "type": "string",
                        "description": "Stable graph node ID from search_in_artefact results."
                    },
                    "position": {
                        "type": "string",
                        "description": "Insert position relative to the target node.",
                        "enum": ["beforebegin", "afterbegin", "beforeend", "afterend"]
                    },
                    "html": {
                        "type": "string",
                        "description": "Raw HTML snippet to insert at the requested position."
                    }
                },
                "required": ["node_id", "position", "html"]
            }
            """));
    }

    internal static AiToolDefinition BuildRemoveElementTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.RemoveElement,
            Description: "Remove a graph node element from a prototype fragment.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "node_id": {
                        "type": "string",
                        "description": "Stable graph node ID from search_in_artefact results."
                    }
                },
                "required": ["node_id"]
            }
            """));
    }

    internal static AiToolDefinition BuildApplyToScopeTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.ApplyToScope,
            Description:
                "Bulk DOM operation — one call, API handles everything.\n\n" +
                "RULES:\n" +
                "- Call once. Do NOT call list_elements or search_in_artefact before this tool.\n" +
                "- API resolves scope, finds elements, generates values, applies, and verifies.\n" +
                "- A partial success is treated as failure. Check the result before claiming done.\n\n" +
                "Strategies:\n" +
                "- literal: same value applied to every matched element. Fast, deterministic, no LLM call.\n" +
                "- derive_from_text_content: API cleans each element's text (strips emoji, arrows, duplicates).\n" +
                "- generate_from_context: one focused LLM call returns [{text_snippet, value}], API matches and applies.\n\n" +
                "Examples:\n" +
                "  Add aria-labels to all buttons: scope=screen-gallery-file, selector=button, operation=set_attribute, attribute=aria-label, strategy=derive_from_text_content\n" +
                "  Add a class to all nav items: scope=shell, selector=.nav-item, operation=add_class, value=btn-primary, strategy=literal",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "scope": {
                        "type": "string",
                        "description": "Fragment scope identifier e.g. screen-gallery-file, shell"
                    },
                    "selector": {
                        "type": "string",
                        "description": "CSS selector to match elements e.g. button, .nav-item, input[type='submit']"
                    },
                    "operation": {
                        "type": "string",
                        "enum": ["set_attribute", "add_class", "remove_class", "swap_class", "set_text", "remove_attribute", "insert_adjacent_html"],
                        "description": "DOM operation to apply to all matched elements. swap_class removes old class and adds new class atomically — value format: old-class:new-class"
                    },
                    "attribute": {
                        "type": "string",
                        "description": "Attribute name for set_attribute or remove_attribute operations (e.g. aria-label, title). For insert_adjacent_html, this is the position: beforebegin, afterbegin, beforeend, or afterend. REQUIRED for insert_adjacent_html."
                    },
                    "strategy": {
                        "type": "string",
                        "enum": ["literal", "derive_from_text_content", "generate_from_context"],
                        "description": "Value derivation strategy. literal=same value to all; derive_from_text_content=API cleans each element text; generate_from_context=one LLM call generates values"
                    },
                    "value": {
                        "type": "string",
                        "description": "Required for literal strategy. The value applied to all matched elements."
                    }
                },
                "required": ["scope", "selector", "operation", "strategy"]
            }
            """));
    }
}
