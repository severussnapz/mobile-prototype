namespace Genesis.AI.Core.Extensions;

/// <summary>
/// String utility extensions.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Returns the string in Unicode Normalisation Form C (NFC).
    /// Ensures emoji and multi-codepoint characters compare correctly
    /// regardless of the form used by the source.
    /// </summary>
    public static string ToNfc(this string value)
    {
        return value.Normalize(System.Text.NormalizationForm.FormC);
    }
}
