namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Validates the structured PR1625 DPIA payload, ensuring all required values and
/// collections are present before a document is generated.
/// </summary>
internal static class Pr1625DpiaValidator
{
    public static void Validate(DpiaData data)
    {
        RequireValue(data.DocumentVersion, "document_version");

        ValidateProject(data.Project);
        ValidateProcessing(data.Processing);
        ValidateDataProfile(data.DataProfile);
        ValidateLegalBasis(data.LegalBasis);
        ValidateRiskAssessment(data.RiskAssessment);
        ValidateSignoff(data.Signoff);
    }

    private static void ValidateProject(DpiaProject project)
    {
        RequireValue(project.Title, "project.title");
        RequireValue(project.RequestDate, "project.request_date");
        RequireValue(project.ContactName, "project.contact_name");
        RequireValue(project.Sponsor, "project.sponsor");
        RequireValue(project.BusinessUnit, "project.business_unit");
        RequireValue(project.Proposition, "project.proposition");
        RequireValue(project.Environment, "project.environment");
        RequireValue(project.ChangeType, "project.change_type");
        RequireValue(project.Summary, "project.summary");
        RequireValue(project.DataFlow, "project.data_flow");
    }

    private static void ValidateProcessing(DpiaProcessing processing)
    {
        RequireValue(processing.Volume, "processing.volume");
        RequireValue(processing.Frequency, "processing.frequency");
        RequireValue(processing.Role, "processing.role");
        RequireValue(processing.DataController, "processing.data_controller");
        RequireNonEmpty(processing.DataSubjects, "processing.data_subjects");
        RequireNonEmpty(processing.Recipients, "processing.recipients");
    }

    private static void ValidateDataProfile(DpiaDataProfile profile)
    {
        RequireNonEmpty(profile.Classifications, "data_profile.classifications");
        RequireNonEmpty(profile.DataCategories, "data_profile.data_categories");
        RequireValue(profile.RetentionRule, "data_profile.retention_rule");
        RequireValue(profile.DeletionTrigger, "data_profile.deletion_trigger");
        RequireNonEmpty(profile.SharingMethods, "data_profile.sharing_methods");
        RequireValue(profile.EncryptionAtRest, "data_profile.encryption_at_rest");
        RequireValue(profile.EncryptionInTransit, "data_profile.encryption_in_transit");
    }

    private static void ValidateLegalBasis(DpiaLegalBasis legalBasis)
    {
        RequireValue(legalBasis.Article6, "legal_basis.article6");
        RequireValue(legalBasis.LawfulPurpose, "legal_basis.lawful_purpose");
        RequireValue(legalBasis.PrivacyNoticeReference, "legal_basis.privacy_notice_reference");
    }

    private static void ValidateRiskAssessment(DpiaRiskAssessment riskAssessment)
    {
        RequireNonEmpty(riskAssessment.Risks, "risk_assessment.risks");
        foreach (var risk in riskAssessment.Risks)
        {
            RequireValue(risk.RiskId, "risk_assessment.risks[].risk_id");
            RequireValue(risk.Title, "risk_assessment.risks[].title");
            RequireValue(risk.Description, "risk_assessment.risks[].description");
            RequireValue(risk.Likelihood, "risk_assessment.risks[].likelihood");
            RequireValue(risk.Impact, "risk_assessment.risks[].impact");
            RequireNonEmpty(risk.Controls, "risk_assessment.risks[].controls");
            RequireValue(risk.ResidualRisk, "risk_assessment.risks[].residual_risk");
            RequireNonEmpty(risk.CheckIds, "risk_assessment.risks[].check_ids");
        }
    }

    private static void ValidateSignoff(DpiaSignoff signoff)
    {
        RequireValue(signoff.IgReviewer, "signoff.ig_reviewer");
        RequireValue(signoff.Role, "signoff.role");
        RequireValue(signoff.Decision, "signoff.decision");
        RequireValue(signoff.Reference, "signoff.reference");
        RequireValue(signoff.Date, "signoff.date");
    }

    private static void RequireValue(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing required value: {name}");
    }

    private static void RequireNonEmpty<T>(IReadOnlyList<T>? values, string name)
    {
        if (values is null || values.Count == 0)
            throw new InvalidOperationException($"Missing required array values: {name}");
    }
}
