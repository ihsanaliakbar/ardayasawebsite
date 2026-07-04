using System.Globalization;
using System.Text;

namespace Ardayasa.Application.Common;

public static class SlugHelper
{
    /// <summary>
    /// Converts a title to a URL slug: lowercase, diacritics stripped,
    /// non-alphanumerics collapsed to single hyphens.
    /// </summary>
    public static string Generate(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        var lastWasHyphen = true; // suppress leading hyphens

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                sb.Append('-');
                lastWasHyphen = true;
            }
        }

        return sb.ToString().TrimEnd('-');
    }
}
