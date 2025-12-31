using Amazon.S3;
using Deepgram;
using Deepgram.Clients.Interfaces.v1;
using Deepgram.Models.Listen.v1.REST;
using Microsoft.Extensions.Options;
using SpeakStoreLocate.ApiService.Models;
using SpeakStoreLocate.ApiService.Options;
using System.Diagnostics;
using System.Text.Json;
using SpeakStoreLocate.ApiService.Utilities;
using System.Globalization;

namespace SpeakStoreLocate.ApiService.Services.Transcription;

public class DeepgramTranscriptionService : ITranscriptionService
{
    private readonly IListenRESTClient _deepgramClient;
    private readonly IAmazonS3 _s3Client;
    private readonly AmazonS3Options _s3Options;
    private readonly ILogger<DeepgramTranscriptionService> _logger;
    private readonly LoggingOptions _loggingOptions;

    public DeepgramTranscriptionService(
        IOptions<DeepgramOptions> deepgramOptions,
        IAmazonS3 s3Client,
        IOptions<AmazonS3Options> s3Options,
        ILogger<DeepgramTranscriptionService> logger,
        IOptions<LoggingOptions> loggingOptions)
    {
        _deepgramClient = ClientFactory.CreateListenRESTClient(deepgramOptions.Value.ApiKey);
        _s3Client = s3Client;
        _s3Options = s3Options.Value;
        _logger = logger;
        _loggingOptions = loggingOptions.Value;
    }

    public async Task<string> TranscriptAudioAsync_Local(AudioUploadRequest request)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            byte[] audioBytes;
            using (var ms = new MemoryStream())
            {
                await request.AudioFile.CopyToAsync(ms);
                audioBytes = ms.ToArray();
            }

            audioBytes = await TryNormalizeAudioAsync(audioBytes, request.AudioFile?.ContentType, request.AudioFile?.FileName);

            _logger.LogDebug("Deepgram local transcription started. ContentType={ContentType} FileName={FileName} AudioBytes={AudioBytes}",
                request.AudioFile?.ContentType,
                request.AudioFile?.FileName,
                audioBytes.Length);

            var response = await _deepgramClient.TranscribeFile(
                audioBytes,
                new PreRecordedSchema()
                {
                    Model = "nova-2",
                    SmartFormat = true,
                    Language = "de",
                });

            var debugPayloadEnabled = _loggingOptions.DebugPayload.Enabled;
            var maxPayloadLength = _loggingOptions.DebugPayload.MaxLength;
            if (debugPayloadEnabled || _logger.IsEnabled(LogLevel.Debug))
            {
                var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
                var truncated = LoggingSanitizer.Truncate(responseJson, maxPayloadLength);
                var suffix = responseJson.Length > maxPayloadLength ? "…(truncated)" : string.Empty;

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Deepgram local response JSON (payload). Response={Response}{Suffix}", truncated, suffix);
                }
                else
                {
                    _logger.LogInformation("Deepgram local response JSON (payload). Response={Response}{Suffix}", truncated, suffix);
                }
            }

            var transcript = response?.Results?.Channels?.FirstOrDefault()?.Alternatives?.FirstOrDefault()?.Transcript ?? string.Empty;
            _logger.LogInformation("Deepgram local transcription finished. TranscriptLength={TranscriptLength} ElapsedMs={ElapsedMs}",
                LoggingSanitizer.SafeLength(transcript),
                sw.ElapsedMilliseconds);
            return transcript;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during local audio transcription");
            throw;
        }
    }

    public async Task<string> TranscriptAudioAsync(AudioUploadRequest request)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            byte[] audioBytes;
            using (var ms = new MemoryStream())
            {
                await request.AudioFile.CopyToAsync(ms);
                audioBytes = ms.ToArray();
            }

            audioBytes = await TryNormalizeAudioAsync(audioBytes, request.AudioFile?.ContentType, request.AudioFile?.FileName);

            _logger.LogDebug("Deepgram transcription started. ContentType={ContentType} FileName={FileName} AudioBytes={AudioBytes}",
                request.AudioFile?.ContentType,
                request.AudioFile?.FileName,
                audioBytes.Length);

            var response = await _deepgramClient.TranscribeFile(
                audioBytes,
                new PreRecordedSchema()
                {
                    Model = "nova-3-general",
                    Punctuate = true,
                    Language = "de",
                    SmartFormat = true,
                });

            var debugPayloadEnabled = _loggingOptions.DebugPayload.Enabled;
            var maxPayloadLength = _loggingOptions.DebugPayload.MaxLength;
            if (debugPayloadEnabled || _logger.IsEnabled(LogLevel.Debug))
            {
                var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
                var truncated = LoggingSanitizer.Truncate(responseJson, maxPayloadLength);
                var suffix = responseJson.Length > maxPayloadLength ? "…(truncated)" : string.Empty;

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Deepgram response JSON (payload). Response={Response}{Suffix}", truncated, suffix);
                }
                else
                {
                    _logger.LogInformation("Deepgram response JSON (payload). Response={Response}{Suffix}", truncated, suffix);
                }
            }

            // 1. Für jeden Kanal die beste Alternative herausholen (null-safe)
            var channels = response?.Results?.Channels;
            if (channels == null || channels.Count == 0)
            {
                _logger.LogWarning("Deepgram response contained no channels.");
                return string.Empty;
            }

            var bestTranscriptionPerChannel = channels
                .Select(channel => channel?.Alternatives)
                .Where(alternatives => alternatives != null && alternatives.Count > 0)
                .Select(alternatives =>
                    alternatives!
                        .OrderByDescending(alt => alt?.Confidence ?? 0)
                        .FirstOrDefault()
                        ?.Transcript)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t!.Trim());

            // 2. Alle Kanal-Transkripte zu einem Gesamtstring verbinden
            string transcriptText = string.Join(" ", bestTranscriptionPerChannel);

            _logger.LogInformation("Deepgram transcription finished. TranscriptLength={TranscriptLength} ElapsedMs={ElapsedMs}",
                LoggingSanitizer.SafeLength(transcriptText),
                sw.ElapsedMilliseconds);
            return transcriptText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio transcription");
            throw;
        }
    }

    private async Task<byte[]> TryNormalizeAudioAsync(byte[] audioBytes, string? contentType, string? fileName)
    {
        try
        {
            if (audioBytes.Length == 0)
            {
                return audioBytes;
            }

            // If it's already WAV, avoid unnecessary work.
            if (IsWav(contentType, fileName))
            {
                return audioBytes;
            }

            var sw = Stopwatch.StartNew();
            var normalized = await FfmpegAudioTranscoder.TranscodeToWavPcm16kMonoAsync(audioBytes);

            _logger.LogDebug(
                "Audio normalized via ffmpeg. OriginalBytes={OriginalBytes} NormalizedBytes={NormalizedBytes} ContentType={ContentType} FileName={FileName} ElapsedMs={ElapsedMs}",
                audioBytes.Length,
                normalized.Length,
                contentType ?? "(null)",
                fileName ?? "(null)",
                sw.ElapsedMilliseconds);

            return normalized;
        }
        catch (Exception ex)
        {
            // Robustness-first: if normalization fails in some environment, keep transcription working.
            _logger.LogWarning(ex,
                "Audio normalization failed; sending original audio to Deepgram. ContentType={ContentType} FileName={FileName} AudioBytes={AudioBytes}",
                contentType ?? "(null)",
                fileName ?? "(null)",
                audioBytes.Length);
            return audioBytes;
        }
    }

    private static bool IsWav(string? contentType, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            var ct = contentType.Trim().ToLower(CultureInfo.InvariantCulture);
            if (ct is "audio/wav" or "audio/x-wav" or "audio/wave" or "audio/vnd.wave")
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}