using System.Text;
using System.Text.RegularExpressions;

namespace SpeakStoreLocate.Tests.Transcription;

public static class TranscriptMetrics
{
    public static string NormalizeForComparison(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Lowercase, remove punctuation-ish characters, collapse whitespace.
        // Keep German umlauts/ß and digits/letters.
        var lower = text.ToLowerInvariant();
        var withoutPunct = Regex.Replace(lower, "[^\\p{L}\\p{Nd}\\s]", " ");
        var collapsed = Regex.Replace(withoutPunct, "\\s+", " ").Trim();
        return collapsed;
    }

    public static double WordErrorRate(string reference, string hypothesis)
    {
        var refNorm = NormalizeForComparison(reference);
        var hypNorm = NormalizeForComparison(hypothesis);

        var refWords = SplitWords(refNorm);
        var hypWords = SplitWords(hypNorm);

        if (refWords.Length == 0)
        {
            return hypWords.Length == 0 ? 0.0 : 1.0;
        }

        var dist = LevenshteinDistance(refWords, hypWords);
        return (double)dist / refWords.Length;
    }

    public static double CharacterErrorRate(string reference, string hypothesis)
    {
        var refNorm = NormalizeForComparison(reference);
        var hypNorm = NormalizeForComparison(hypothesis);

        if (refNorm.Length == 0)
        {
            return hypNorm.Length == 0 ? 0.0 : 1.0;
        }

        var dist = LevenshteinDistance(refNorm.AsSpan(), hypNorm.AsSpan());
        return (double)dist / refNorm.Length;
    }

    private static string[] SplitWords(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized)) return Array.Empty<string>();
        return normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    // Levenshtein on word arrays
    private static int LevenshteinDistance(string[] a, string[] b)
    {
        var n = a.Length;
        var m = b.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        var prev = new int[m + 1];
        var curr = new int[m + 1];

        for (var j = 0; j <= m; j++) prev[j] = j;

        for (var i = 1; i <= n; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= m; j++)
            {
                var cost = string.Equals(a[i - 1], b[j - 1], StringComparison.Ordinal) ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1, prev[j] + 1),
                    prev[j - 1] + cost);
            }

            (prev, curr) = (curr, prev);
        }

        return prev[m];
    }

    // Levenshtein on spans (chars)
    private static int LevenshteinDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        var n = a.Length;
        var m = b.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        var prev = new int[m + 1];
        var curr = new int[m + 1];

        for (var j = 0; j <= m; j++) prev[j] = j;

        for (var i = 1; i <= n; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= m; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1, prev[j] + 1),
                    prev[j - 1] + cost);
            }

            (prev, curr) = (curr, prev);
        }

        return prev[m];
    }
}
