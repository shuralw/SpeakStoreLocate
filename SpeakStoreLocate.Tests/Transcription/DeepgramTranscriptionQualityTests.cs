using System.Net.Http.Headers;
using System.Text.Json;
using SpeakStoreLocate.ApiService.Utilities;
using Xunit.Abstractions;

namespace SpeakStoreLocate.Tests.Transcription;

public class DeepgramTranscriptionQualityTests
{
    private readonly ITestOutputHelper _output;

    public DeepgramTranscriptionQualityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object?[]> AssetCases
    {
        get
        {
            var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
            if (!Directory.Exists(assetsDir))
            {
                yield return new object?[] { "(no-assets)", null, null };
                yield break;
            }

            var expectedFiles = Directory.GetFiles(assetsDir, "*.txt", SearchOption.TopDirectoryOnly);
            if (expectedFiles.Length == 0)
            {
                yield return new object?[] { "(no-assets)", null, null };
                yield break;
            }

            var emittedAny = false;
            foreach (var expectedPath in expectedFiles)
            {
                var baseName = Path.GetFileNameWithoutExtension(expectedPath);
                var audioPath = FindAudioForBaseName(assetsDir, baseName);
                if (audioPath == null)
                {
                    continue;
                }

                emittedAny = true;
                yield return new object?[] { baseName, expectedPath, audioPath };
            }

            if (!emittedAny)
            {
                yield return new object?[] { "(no-matching-audio)", null, null };
            }
        }
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(AssetCases))]
    public async Task Deepgram_DeAssets_WerBelowThreshold_PerCase(string caseName, string? expectedPath, string? audioPath)
    {
        var apiKey = Environment.GetEnvironmentVariable("DEEPGRAM_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _output.WriteLine("Skipping '{0}': DEEPGRAM_API_KEY not set.", caseName);
            return;
        }

        if (string.IsNullOrWhiteSpace(expectedPath) || string.IsNullOrWhiteSpace(audioPath))
        {
            _output.WriteLine("Skipping '{0}': no asset pair available.", caseName);
            return;
        }

        var werThreshold = ReadDoubleEnv("STT_WER_THRESHOLD", 0.20);

        var expected = await File.ReadAllTextAsync(expectedPath);
        var rawAudioBytes = await File.ReadAllBytesAsync(audioPath);

        // 1) Baseline: send raw audio as-is to Deepgram
        var rawContentType = GuessContentTypeFromPath(audioPath);
        var transcriptRaw = await TranscribeWithDeepgramAsync(apiKey, rawAudioBytes, contentType: rawContentType);

        var werRaw = TranscriptMetrics.WordErrorRate(expected, transcriptRaw);
        var cerRaw = TranscriptMetrics.CharacterErrorRate(expected, transcriptRaw);

        // 2) Normalized: WAV/PCM 16k mono
        byte[] wavBytes;
        try
        {
            wavBytes = await FfmpegAudioTranscoder.TranscodeToWavPcm16kMonoAsync(rawAudioBytes);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to start process 'ffmpeg'", StringComparison.OrdinalIgnoreCase))
        {
            _output.WriteLine("Skipping '{0}': ffmpeg not available. {1}", caseName, ex.Message);
            return;
        }

        var transcriptNorm = await TranscribeWithDeepgramAsync(apiKey, wavBytes, contentType: "audio/wav");
        var werNorm = TranscriptMetrics.WordErrorRate(expected, transcriptNorm);
        var cerNorm = TranscriptMetrics.CharacterErrorRate(expected, transcriptNorm);

        _output.WriteLine("Case: {0}", caseName);
        _output.WriteLine("  Raw   : WER={0:0.000} CER={1:0.000} ContentType={2}", werRaw, cerRaw, rawContentType);
        _output.WriteLine("  Norm  : WER={0:0.000} CER={1:0.000} ContentType=audio/wav", werNorm, cerNorm);
        _output.WriteLine("  Expect: {0}", TranscriptMetrics.NormalizeForComparison(expected));
        _output.WriteLine("  RawTx : {0}", TranscriptMetrics.NormalizeForComparison(transcriptRaw));
        _output.WriteLine("  NormTx: {0}", TranscriptMetrics.NormalizeForComparison(transcriptNorm));

        // Assert only on normalized path (primary pipeline after server-side normalization)
        Assert.True(
            werNorm <= werThreshold,
            $"STT quality regression for '{caseName}'. Normalized WER={werNorm:0.000} (threshold {werThreshold:0.000}), CER={cerNorm:0.000}.\n" +
            $"Expected(norm): '{TranscriptMetrics.NormalizeForComparison(expected)}'\n" +
            $"Actual(norm):   '{TranscriptMetrics.NormalizeForComparison(transcriptNorm)}'\n");
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

    private static string GuessContentTypeFromPath(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".webm" => "audio/webm",
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".ogg" => "audio/ogg",
            _ => "application/octet-stream",
        };
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
