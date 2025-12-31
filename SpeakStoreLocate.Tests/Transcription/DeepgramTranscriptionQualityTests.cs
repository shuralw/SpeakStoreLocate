using System.Net.Http.Headers;
using System.Text.Json;
using SpeakStoreLocate.ApiService.Utilities;

namespace SpeakStoreLocate.Tests.Transcription;

public class DeepgramTranscriptionQualityTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Deepgram_DeAssets_WerBelowThreshold()
    {
        var apiKey = Environment.GetEnvironmentVariable("DEEPGRAM_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return; // integration test requires external config
        }

        var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        if (!Directory.Exists(assetsDir))
        {
            return;
        }

        // Find all pairs: <name>.(webm|wav|mp3|m4a|ogg) + <name>.txt
        var expectedFiles = Directory.GetFiles(assetsDir, "*.txt", SearchOption.TopDirectoryOnly);
        if (expectedFiles.Length == 0)
        {
            return;
        }

        var werThreshold = ReadDoubleEnv("STT_WER_THRESHOLD", 0.20);

        var executedAnyCase = false;
        foreach (var expectedPath in expectedFiles)
        {
            var baseName = Path.GetFileNameWithoutExtension(expectedPath);
            var audioPath = FindAudioForBaseName(assetsDir, baseName);
            if (audioPath == null)
            {
                continue;
            }

            executedAnyCase = true;

            var expected = await File.ReadAllTextAsync(expectedPath);
            var audioBytes = await File.ReadAllBytesAsync(audioPath);

            // Always normalize to WAV/PCM 16k mono to reduce variability.
            byte[] wavBytes;
            try
            {
                wavBytes = await FfmpegAudioTranscoder.TranscodeToWavPcm16kMonoAsync(audioBytes);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to start process 'ffmpeg'", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            var transcript = await TranscribeWithDeepgramAsync(apiKey, wavBytes, contentType: "audio/wav");

            var wer = TranscriptMetrics.WordErrorRate(expected, transcript);
            var cer = TranscriptMetrics.CharacterErrorRate(expected, transcript);

            Assert.True(
                wer <= werThreshold,
                $"STT quality regression for '{baseName}'. WER={wer:0.000} (threshold {werThreshold:0.000}), CER={cer:0.000}.\n" +
                $"Expected(norm): '{TranscriptMetrics.NormalizeForComparison(expected)}'\n" +
                $"Actual(norm):   '{TranscriptMetrics.NormalizeForComparison(transcript)}'\n");
        }

        if (!executedAnyCase)
        {
            return;
        }
    }

    private static async Task<string> TranscribeWithDeepgramAsync(string apiKey, byte[] audioBytes, string contentType)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", apiKey);

        // Keep settings minimal & stable: German, punctuation, smart_format.
        var url = "https://api.deepgram.com/v1/listen?model=nova-3-general&language=de&punctuate=true&smart_format=true";

        using var content = new ByteArrayContent(audioBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var response = await http.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return ExtractTranscript(json);
    }

    private static string ExtractTranscript(string json)
    {
        using var doc = JsonDocument.Parse(json);

        // results.channels[0].alternatives[0].transcript
        if (!doc.RootElement.TryGetProperty("results", out var results)) return string.Empty;
        if (!results.TryGetProperty("channels", out var channels) || channels.ValueKind != JsonValueKind.Array || channels.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var channel0 = channels[0];
        if (!channel0.TryGetProperty("alternatives", out var alternatives) || alternatives.ValueKind != JsonValueKind.Array || alternatives.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var alt0 = alternatives[0];
        if (!alt0.TryGetProperty("transcript", out var transcriptProp)) return string.Empty;
        return transcriptProp.GetString() ?? string.Empty;
    }

    private static string? FindAudioForBaseName(string assetsDir, string baseName)
    {
        var exts = new[] { ".webm", ".wav", ".mp3", ".m4a", ".ogg" };
        foreach (var ext in exts)
        {
            var candidate = Path.Combine(assetsDir, baseName + ext);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    private static double ReadDoubleEnv(string name, double defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
        return double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v
            : defaultValue;
    }
}
