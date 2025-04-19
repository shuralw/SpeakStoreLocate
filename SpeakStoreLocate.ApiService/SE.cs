using System.Globalization;
using System.Text;

public static class StringExtensions
{
    public static string NormalizeForSearch(this string input)
    {
        if (input == null) return null!;
        // FormD zerlegt Buchstaben + Diakritika
        var formD = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in formD)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            // alle Nicht‑Spacing‑Marks behalten wir nicht
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        // wieder zusammensetzen und klein machen
        return sb
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }
}