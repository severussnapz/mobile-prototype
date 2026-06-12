using System.Text.Json;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

internal static class ArtefactToolBuilder
{
    internal static AiToolDefinition BuildSaveArtefactTool()
    {
        return new AiToolDefinition(
            Name: PipelineToolDefinitions.SaveArtefact,
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
                        // guardrail:skip=SEC-002:JSON schema literal, not SQL
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
                         "IMPORTANT: Before calling this, always fetch the file fresh with get_artefact — your cached content may be stale. " +
                         "On ANCHOR_NOT_FOUND or ANCHOR_AMBIGUOUS errors, re-read the file and retry (maximum 2 retries).",
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
                        "description": "The exact string to find and replace. Must appear exactly once in the file."
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
}
