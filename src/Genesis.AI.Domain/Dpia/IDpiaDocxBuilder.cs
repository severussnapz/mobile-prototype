namespace Genesis.AI.Domain.Dpia;

/// <summary>
/// Builds a PR1625 DPIA Word document (.docx) from structured DPIA JSON data.
/// </summary>
public interface IDpiaDocxBuilder
{
    /// <summary>
    /// Produces the populated PR1625 DPIA document as a byte array.
    /// </summary>
    /// <param name="dpiaJson">The source JSON payload (output/PR1625_DPIA_DATA.json).</param>
    byte[] Build(string dpiaJson);
}
