using NpgsqlTypes;

namespace Genesis.AI.Domain.Enums;

public enum KnowledgeNamespace
{
    [PgName("genesis_tool")]
    GenesisTool,

    [PgName("project_artefact")]
    ProjectArtefact
}