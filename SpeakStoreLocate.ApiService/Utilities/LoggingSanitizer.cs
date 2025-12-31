namespace SpeakStoreLocate.ApiService.Utilities;

internal static class LoggingSanitizer
{
    public static int SafeLength(string? value) => value?.Length ?? 0;

    public static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (maxLength <= 0)
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
