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
                         "Use this for changes affecting less than ~30% of a file — far cheaper than regenerating the whole file. " +
                         "IMPORTANT: Before calling this, use search_in_artefact to find the exact text to anchor against — do NOT guess or reconstruct from memory. " +
                         "On ANCHOR_NOT_FOUND or ANCHOR_AMBIGUOUS errors, search_in_artefact again and retry (maximum 2 retries).",
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
}
