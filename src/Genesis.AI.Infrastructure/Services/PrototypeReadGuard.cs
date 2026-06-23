using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Structural guard on the get_artefact read path. Once a prototype is built
/// (prototype/fragments/_shell.html exists), requirements/* files are unreadable during
/// the Prototype stage — an edit operates on the fragments, never on the requirements.
/// </summary>
public static class PrototypeReadGuard
{
    private const string RequirementsPrefix = "requirements/";

    public static string? ValidateGetArtefact(
        StageType? stageType,
        string filePath,
        bool prototypeAlreadyBuilt)
    {
        if (stageType != StageType.Prototype || !prototypeAlreadyBuilt)
        {
            return null;
        }

        if (!filePath.StartsWith(RequirementsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return "NOTHING WAS READ. requirements/* cannot be read while editing an already-built prototype. " +
               "The design is captured in the prototype/fragments/* files — use list_artefacts, then " +
               "search_in_artefact and apply_to_scope to make the change.";
    }
}
