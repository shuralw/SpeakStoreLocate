namespace SpeakStoreLocate.ApiService.Options;

public sealed class LoggingOptions
{
    public FileLoggingOptions File { get; init; } = new();
    public DebugPayloadLoggingOptions DebugPayload { get; init; } = new();

    public sealed class FileLoggingOptions
    {
        public bool Enabled { get; init; }
        public string Path { get; init; } = "logs/prod-.log";
        public string RollingInterval { get; init; } = "Day";
        public int RetainedFileCountLimit { get; init; } = 14;
        public string OutputTemplate { get; init; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
    }

    public sealed class DebugPayloadLoggingOptions
    {
        public bool Enabled { get; init; }
        public int MaxLength { get; init; } = 20000;
    }
}
