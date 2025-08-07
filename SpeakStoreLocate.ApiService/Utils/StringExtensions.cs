using System.Globalization;
using System.Text;

namespace SpeakStoreLocate.ApiService.Utils;

public static class StringExtensions
{
    public static string NormalizeForSearch(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // 1. Auf Lowercase
        var s = input.ToLowerInvariant();

        // 2. Deutsche Umlaute & ß ersetzen
        s = s
            .Replace("ä", "ae")
            .Replace("ö", "oe")
            .Replace("ü", "ue")
            .Replace("ß", "ss");

        // 3. Andere Diakritika entfernen
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in formD)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        // 4. Wieder zusammensetzen
        return sb
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}