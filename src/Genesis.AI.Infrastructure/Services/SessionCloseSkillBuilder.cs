using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class SessionCloseSkillBuilder : ISessionCloseSkillBuilder
{
    private static readonly Dictionary<StageType, (string Code, string Name)> StageInfo =
        new()
        {
            [StageType.RequirementsDiscovery] = ("P01", "Requirements Discovery"),
            [StageType.Prototype] = ("P02", "Prototype"),
            [StageType.Architecture] = ("P03", "Architecture"),
            [StageType.Design] = ("P04", "Design"),
            [StageType.Pxd] = ("P05", "PxD"),
            [StageType.ClinicalSafety] = ("P06", "Clinical Safety"),
            [StageType.InformationGovernance] = ("P07", "Information Governance"),
            [StageType.Security] = ("P08", "Security"),
        };

    public string Build(StageType stageType, string conversationSummary)
    {
        var (code, name) = StageInfo.TryGetValue(stageType, out var info)
            ? info
            : throw new NotSupportedException($"Stage type {stageType} not supported.");

        return $"""
            You are generating a SESSION-CLOSE document for pipeline stage {code} - {name}.

            Produce a structured Markdown document that summarises what was achieved,
            what decisions were made, what artefacts were produced, and what actions
            are required before the next stage begins.

            ## Conversation Summary

            {conversationSummary}

            ## Required Sections

            - ## Session Overview
            - ## Decisions Made
            - ## Artefacts Produced
            - ## Open Items
            - ## Next Stage Prerequisites
            """;
    }
}