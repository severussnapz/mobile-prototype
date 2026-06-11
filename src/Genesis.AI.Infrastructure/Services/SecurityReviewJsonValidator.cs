using System.Text.Json;
using System.Text.RegularExpressions;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Validates the security assurance and SDP evidence JSON payloads against the
/// expected security review schema before a workbook is generated.
/// </summary>
internal static class SecurityReviewJsonValidator
{
    private static readonly HashSet<string> AttackVectorStatuses = new(StringComparer.Ordinal)
    {
        "covered",
        "partial",
        "not_applicable",
        "gap"
    };

    private static readonly HashSet<string> SdpStatuses = new(StringComparer.Ordinal)
    {
        "pass",
        "partial",
        "fail",
        "not_applicable"
    };

    private static readonly HashSet<string> CheckTypes = new(StringComparer.Ordinal)
    {
        "positive",
        "negative",
        "abuse",
        "evidence"
    };

    private static readonly HashSet<string> EvidenceTypes = new(StringComparer.Ordinal)
    {
        "ci_report",
        "policy_doc",
        "platform_setting",
        "audit_log",
        "manual_attestation"
    };

    private static readonly HashSet<string> SignoffDecisions = new(StringComparer.Ordinal)
    {
        "approved",
        "approved_with_conditions",
        "blocked"
    };

    public static void ValidateSecurityAssurance(JsonElement root)
    {
        SecurityReviewJson.RequireObject(root, "<root>");
        SecurityReviewJson.RequireAllowedProperties(root, "<root>", [
            "document_version",
            "project",
            "threat_model",
            "attack_vector_coverage",
            "control_mappings",
            "checks",
            "evidence_artifacts",
            "review_signoff"
        ]);

        SecurityReviewJson.RequireString(root, "document_version");
        ValidateProject(root);
        ValidateThreatModel(root);
        ValidateAttackVectorCoverage(root);
        ValidateControlMappings(root);
        ValidateChecks(root);
        ValidateEvidenceArtifacts(root);
        ValidateReviewSignoff(root);
    }

    public static void ValidateSdpEvidence(JsonElement root)
    {
        SecurityReviewJson.RequireObject(root, "<root>");
        SecurityReviewJson.RequireAllowedProperties(root, "<root>", ["project_code", "generated_at", "controls"]);

        SecurityReviewJson.RequireString(root, "project_code");
        SecurityReviewJson.RequireString(root, "generated_at");

        var controls = SecurityReviewJson.RequireArrayProperty(root, "controls");
        var seenControlIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var control in controls.EnumerateArray())
        {
            ValidateSdpControl(control, seenControlIds);
        }
    }

    private static void ValidateProject(JsonElement root)
    {
        var project = SecurityReviewJson.RequireObjectProperty(root, "project");
        SecurityReviewJson.RequireAllowedProperties(project, "project", ["name", "summary", "architecture_context", "data_sensitivity"]);
        SecurityReviewJson.RequireString(project, "name");
        SecurityReviewJson.RequireString(project, "summary");
        SecurityReviewJson.RequireString(project, "architecture_context");
        SecurityReviewJson.RequireOptionalStringArray(project, "data_sensitivity");
    }

    private static void ValidateThreatModel(JsonElement root)
    {
        var threatModel = SecurityReviewJson.RequireObjectProperty(root, "threat_model");
        SecurityReviewJson.RequireAllowedProperties(threatModel, "threat_model", ["assets", "actors", "entry_points", "abuse_cases"]);
        SecurityReviewJson.RequireStringArray(threatModel, "assets");
        SecurityReviewJson.RequireStringArray(threatModel, "actors");
        SecurityReviewJson.RequireStringArray(threatModel, "entry_points");
        SecurityReviewJson.RequireStringArray(threatModel, "abuse_cases");
    }

    private static void ValidateAttackVectorCoverage(JsonElement root)
    {
        var attackVectorCoverage = SecurityReviewJson.RequireObjectProperty(root, "attack_vector_coverage");
        SecurityReviewJson.RequireAllowedProperties(attackVectorCoverage, "attack_vector_coverage", [
            "repo_secrets",
            "ci_cd_exposure",
            "supply_chain",
            "injection",
            "authn_authz",
            "crypto",
            "logging_monitoring"
        ]);

        ValidateAttackVector(attackVectorCoverage, "repo_secrets");
        ValidateAttackVector(attackVectorCoverage, "ci_cd_exposure");
        ValidateAttackVector(attackVectorCoverage, "supply_chain");
        ValidateAttackVector(attackVectorCoverage, "injection");
        ValidateAttackVector(attackVectorCoverage, "authn_authz");
        ValidateAttackVector(attackVectorCoverage, "crypto");
        ValidateAttackVector(attackVectorCoverage, "logging_monitoring");
    }

    private static void ValidateControlMappings(JsonElement root)
    {
        var controlMappings = SecurityReviewJson.RequireArrayProperty(root, "control_mappings");
        foreach (var mapping in controlMappings.EnumerateArray())
        {
            SecurityReviewJson.RequireAllowedProperties(mapping, "control_mappings[]", [
                "control_id",
                "title",
                "owasp",
                "asvs",
                "cwe",
                "internal_policy_refs",
                "applicability_rationale",
                "requirement_ids"
            ]);

            SecurityReviewJson.RequireString(mapping, "control_id");
            SecurityReviewJson.RequireString(mapping, "title");
            SecurityReviewJson.RequireStringArray(mapping, "owasp");
            SecurityReviewJson.RequireOptionalStringArray(mapping, "asvs");
            SecurityReviewJson.RequireOptionalStringArray(mapping, "cwe");
            SecurityReviewJson.RequireStringArray(mapping, "internal_policy_refs");
            SecurityReviewJson.RequireString(mapping, "applicability_rationale");
            SecurityReviewJson.RequireStringArray(mapping, "requirement_ids");
        }
    }

    private static void ValidateChecks(JsonElement root)
    {
        var checks = SecurityReviewJson.RequireArrayProperty(root, "checks");
        foreach (var check in checks.EnumerateArray())
        {
            SecurityReviewJson.RequireAllowedProperties(check, "checks[]", ["check_id", "control_id", "test_type", "scenario", "pass_criteria", "evidence_ref", "requirement_id"]);
            SecurityReviewJson.RequireString(check, "check_id");
            SecurityReviewJson.RequireString(check, "control_id");
            SecurityReviewJson.RequireEnumValue(check, "test_type", CheckTypes);
            SecurityReviewJson.RequireString(check, "scenario");
            SecurityReviewJson.RequireString(check, "pass_criteria");
            SecurityReviewJson.RequireString(check, "evidence_ref");
            SecurityReviewJson.RequireOptionalString(check, "requirement_id");
        }
    }

    private static void ValidateEvidenceArtifacts(JsonElement root)
    {
        var evidenceArtifacts = SecurityReviewJson.RequireArrayProperty(root, "evidence_artifacts");
        foreach (var artifact in evidenceArtifacts.EnumerateArray())
        {
            SecurityReviewJson.RequireAllowedProperties(artifact, "evidence_artifacts[]", ["artifact_id", "type", "location", "description"]);
            SecurityReviewJson.RequireString(artifact, "artifact_id");
            SecurityReviewJson.RequireString(artifact, "type");
            SecurityReviewJson.RequireString(artifact, "location");
            SecurityReviewJson.RequireString(artifact, "description");
        }
    }

    private static void ValidateReviewSignoff(JsonElement root)
    {
        var signOff = SecurityReviewJson.RequireObjectProperty(root, "review_signoff");
        SecurityReviewJson.RequireAllowedProperties(signOff, "review_signoff", ["reviewer", "role", "decision", "reference", "date"]);
        SecurityReviewJson.RequireString(signOff, "reviewer");
        SecurityReviewJson.RequireString(signOff, "role");
        SecurityReviewJson.RequireEnumValue(signOff, "decision", SignoffDecisions);
        SecurityReviewJson.RequireString(signOff, "reference");
        SecurityReviewJson.RequireString(signOff, "date");
    }

    private static void ValidateSdpControl(JsonElement control, HashSet<string> seenControlIds)
    {
        SecurityReviewJson.RequireAllowedProperties(control, "controls[]", [
            "control_id",
            "name",
            "status",
            "evidence_type",
            "evidence_ref",
            "owner",
            "last_reviewed",
            "notes",
            "mapped_requirements"
        ]);

        var controlId = SecurityReviewJson.RequireString(control, "control_id");
        if (!Regex.IsMatch(controlId, "^SDP-[0-9]{3}$", RegexOptions.CultureInvariant))
        {
            throw new InvalidOperationException($"Invalid control_id format: {controlId}");
        }

        if (!seenControlIds.Add(controlId))
        {
            throw new InvalidOperationException($"Duplicate SDP control_id: {controlId}");
        }

        SecurityReviewJson.RequireString(control, "name");
        var status = SecurityReviewJson.RequireEnumValue(control, "status", SdpStatuses);
        if (string.Equals(status, "fail", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"SDP control '{controlId}' is marked fail.");
        }

        SecurityReviewJson.RequireEnumValue(control, "evidence_type", EvidenceTypes);
        SecurityReviewJson.RequireString(control, "evidence_ref");
        SecurityReviewJson.RequireString(control, "owner");
        SecurityReviewJson.RequireString(control, "last_reviewed");
        SecurityReviewJson.RequireOptionalString(control, "notes");
        SecurityReviewJson.RequireOptionalStringArray(control, "mapped_requirements");
    }

    private static void ValidateAttackVector(JsonElement attackVectorCoverage, string propertyName)
    {
        var vector = attackVectorCoverage.GetProperty(propertyName);
        SecurityReviewJson.RequireAllowedProperties(vector, $"attack_vector_coverage.{propertyName}", ["status", "controls", "evidence_refs"]);
        SecurityReviewJson.RequireEnumValue(vector, "status", AttackVectorStatuses);
        SecurityReviewJson.RequireStringArray(vector, "controls");
        SecurityReviewJson.RequireStringArray(vector, "evidence_refs");
    }
}
