using System.Reflection;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Genesis.AI.Domain.Dpia;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Builds PR1625 Data Protection Impact Assessment documents from structured JSON,
/// using the embedded PR1625 Word template.
/// </summary>
public sealed class Pr1625DpiaDocxBuilder : IDpiaDocxBuilder
{
    private const string TemplateResourceName =
        "Genesis.AI.Infrastructure.Resources.Pr1625DataProtectionImpactAssessmentTemplate.docx";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public byte[] Build(string dpiaJson)
    {
        if (string.IsNullOrWhiteSpace(dpiaJson))
            throw new InvalidOperationException("DPIA JSON payload is empty.");

        var data = JsonSerializer.Deserialize<DpiaData>(dpiaJson, JsonOptions)
            ?? throw new InvalidOperationException("Unable to parse DPIA JSON payload.");

        Pr1625DpiaValidator.Validate(data);

        using var templateStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {TemplateResourceName}");

        using var outputStream = new MemoryStream();
        templateStream.CopyTo(outputStream);
        outputStream.Position = 0;

        using (var document = WordprocessingDocument.Open(outputStream, true))
        {
            var body = document.MainDocumentPart?.Document.Body
                ?? throw new InvalidOperationException("Invalid template: document body missing.");

            var tables = body.Elements<Table>().ToList();
            if (tables.Count < 12)
                throw new InvalidOperationException("Invalid PR1625 template: expected at least 12 tables.");

            Pr1625DpiaTableWriter.PopulateTemplateTables(tables, data);
            Pr1625DpiaTableWriter.AppendGeneratedMappingSection(body, data);
            document.MainDocumentPart!.Document.Save();
        }

        return outputStream.ToArray();
    }
}
