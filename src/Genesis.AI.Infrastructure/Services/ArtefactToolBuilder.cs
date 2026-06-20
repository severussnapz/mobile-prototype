using System.Text.Json;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

internal static class ArtefactToolBuilder
{
    internal static AiToolDefinition BuildSaveArtefactTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.SaveArtefact,
            Description: """Save a file artefact (e.g. manifest.md, requirements/REQ-001.md) to the project's artefact store. Call this whenever you produce a complete file output. If the same file_path is saved again, it creates a new version. Write your full response text first, then call this tool — you will not get another turn. ⚠️ For large files (>10KB), prefer using edit_artefact for surgical changes instead — it will succeed where save_artefact runs out of tokens. Only use save_artefact for new files or complete regeneration of small files.""",
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
            """));
    }

    internal static AiToolDefinition BuildListArtefactsTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.ListArtefacts,
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
            """));
    }

    internal static AiToolDefinition BuildGetArtefactTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.GetArtefact,
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
            """));
    }

    internal static AiToolDefinition BuildEditArtefactTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.EditArtefact,
            Description: "Make a surgical edit to an existing artefact by replacing an exact anchor string with new content. " +
                         "Use this for REQ file changes affecting less than ~30% of the file — far cheaper than regenerating the whole file. " +
                         "IMPORTANT: Before calling this, use search_in_artefact to find the exact text to anchor against — do NOT guess or reconstruct from memory. " +
                         "On ANCHOR_NOT_FOUND or ANCHOR_AMBIGUOUS, search_in_artefact again with a different keyword and retry (maximum 2 retries). " +
                         "NOT for use on Prototype HTML — use edit_artefact_by_graph_node on prototype/index.html instead.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "file_path": {
                        "type": "string",
                        "description": "The relative file path of the artefact to edit."
                    },
                    "old_str": {
                        "type": "string",
                        "description": "The exact verbatim string to find and replace. Must appear exactly once in the file. Copy it character-for-character from search_in_artefact results."
                    },
                    "new_str": {
                        "type": "string",
                        "description": "The replacement string. Empty string to delete the anchor."
                    }
                },
                "required": ["file_path", "old_str", "new_str"]
            }
            """));
    }

    internal static AiToolDefinition BuildSearchInArtefactTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.SearchInArtefact,
            Description: "Search for lines in an artefact file that contain a given query string. " +
                         "Returns up to 5 matching regions with ±5 lines of surrounding context each. " +
                         "Use this BEFORE edit_artefact to find the exact verbatim anchor string to pass as old_str — " +
                         "copy the anchor directly from the search results, never from memory.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "file_path": {
                        "type": "string",
                        "description": "The relative file path of the artefact to search."
                    },
                    "query": {
                        "type": "string",
                        "description": "A keyword or short phrase to search for (case-insensitive). Use a distinctive word from the content you want to change, e.g. 'nav', 'background', 'header'."
                    }
                },
                "required": ["file_path", "query"]
            }
            """));
    }

    internal static AiToolDefinition BuildEditArtefactByGraphNodeTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.EditArtefactByGraphNode,
            Description: "Edit an HTML artefact by graph node reference. Use this when a prototype graph node_id is known. " +
                         "The runtime resolves the exact anchor snippet from the graph and applies a single surgical replacement. " +
                         "CRITICAL: new_str must be a surgical modification of the EXISTING element — do NOT wrap it in new containers or change its outer structure. " +
                         "The replacement must preserve the element's id attribute exactly as-is (e.g. id=\"<node_id>\"). " +
                         "To add a tooltip: add a title attribute or a sibling tooltip element INSIDE the existing element — do not replace the element itself.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "file_path": {
                        "type": "string",
                        "description": "The relative file path of the HTML artefact to edit."
                    },
                    "node_id": {
                        "type": "string",
                        "description": "Stable graph node ID from the prototype graph index."
                    },
                    "new_str": {
                        "type": "string",
                        "description": "Replacement HTML for the referenced node. Must preserve the element's id attribute exactly (id=\"<node_id>\"). Make surgical changes only — do not rewrap the element in new containers."
                    }
                },
                "required": ["file_path", "node_id", "new_str"]
            }
            """));
    }

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

    internal static AiToolDefinition BuildListElementsTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.ListElements,
            Description: "Returns ALL elements matching a CSS selector in the prototype, optionally scoped to a specific container node.\n\n" +
                         "TWO-STEP WORKFLOW FOR SCOPED SEARCHES:\n" +
                         "Step 1: Use search_in_artefact to find the container element\n" +
                         "  e.g. search_in_artefact('screen-gallery-file') to find the\n" +
                         "  document viewer screen — note the node_id returned.\n" +
                         "Step 2: Call list_elements with that node_id as scope_node_id\n" +
                         "  e.g. list_elements(selector='button',\n" +
                         "       scope_node_id='prototype/fragments/screen-01-legacy.html|ABC123')\n" +
                         "  This returns ONLY buttons inside that container.\n\n" +
                         "Without scope_node_id: returns all matching elements across\n" +
                         "the entire prototype (may return hundreds — use with caution).\n" +
                         "With scope_node_id: returns only elements inside that container.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "selector": {
                        "type": "string",
                        "description": "CSS selector to match elements, e.g. 'button', '.btn', \"input[type='submit']\"."
                    },
                    "scope_node_id": {
                        "type": "string",
                        "description": "Optional node_id (NodeKey format fragmentPath|stableId) from a previous search result. If provided, matches are scoped to descendants of that node; otherwise searches whole prototype."
                    }
                },
                "required": ["selector"]
            }
            """));
    }

    internal static AiToolDefinition BuildApplyBulkAttributesTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.ApplyBulkAttributes,
            Description: "WORKFLOW:\n" +
                         "1. Run list_elements to get all target text_snippet, parent, and siblings context.\n" +
                         "2. Derive one value per node from that context.\n" +
                         "3. Call apply_bulk_attributes ONCE with attribute and snippet_value_pairs.\n" +
                         "4. Do NOT call set_node_attribute for this batch.\n" +
                         "This tool applies all mutations and reassembles once at the end.",
            InputSchema: JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "attribute": {
                        "type": "string",
                        "description": "Attribute name applied to all snippet_value_pairs, e.g. aria-label, title, placeholder."
                    },
                    "snippet_value_pairs": {
                        "type": "array",
                        "description": "List of text_snippet to value pairs in list_elements order.",
                        "items": {
                            "type": "object",
                            "properties": {
                                "text_snippet": {
                                    "type": "string",
                                    "description": "Text snippet copied from list_elements output."
                                },
                                "value": {
                                    "type": "string",
                                    "description": "Attribute value to set."
                                }
                            },
                            "required": ["text_snippet", "value"]
                        }
                    }
                },
                "required": ["attribute", "snippet_value_pairs"]
            }
            """));
    }
}
