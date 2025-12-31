using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SpeakStoreLocate.ApiService.Models;
using SpeakStoreLocate.ApiService.Options;
using System.Diagnostics;
using SpeakStoreLocate.ApiService.Utilities;

namespace SpeakStoreLocate.ApiService.Services.Transcription;

public class ElevenlabsTranscriptionService : ITranscriptionService
{
    private readonly ILogger<ElevenlabsTranscriptionService> _logger;
    private readonly HttpClient _elevenlabsHttpClient;
    private readonly ElevenLabsOptions _options;
    private readonly LoggingOptions _loggingOptions;

    public ElevenlabsTranscriptionService(
        IOptions<ElevenLabsOptions> options,
        ILogger<ElevenlabsTranscriptionService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<LoggingOptions> loggingOptions)
    {
        _logger = logger;
        _options = options.Value;
        _loggingOptions = loggingOptions.Value;
        
        _elevenlabsHttpClient = httpClientFactory.CreateClient("ElevenLabs");
        _elevenlabsHttpClient.BaseAddress = new Uri(_options.BaseUrl ?? "https://api.elevenlabs.io");
        _elevenlabsHttpClient.DefaultRequestHeaders.Clear();
        _elevenlabsHttpClient.DefaultRequestHeaders.Add("xi-api-key", _options.ApiKey);
    }

    public async Task<string> TranscriptAudioAsync(AudioUploadRequest request)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            // 1) Datei in MemoryStream laden
            using var ms = new MemoryStream();
            await request.AudioFile.CopyToAsync(ms);
            ms.Position = 0;

            _logger.LogDebug("ElevenLabs transcription started. ContentType={ContentType} FileName={FileName} FileLength={FileLength}",
                request.AudioFile?.ContentType,
                request.AudioFile?.FileName,
                request.AudioFile?.Length);

            // 2) Multipart/FormData-Content aufbauen
            using var content = new MultipartFormDataContent();
            // ⟶ unbedingt model_id mitsenden
            content.Add(new StringContent("scribe_v1"), "model_id");
            // ⟶ optional Sprache angeben
            content.Add(new StringContent("de"), "language_code");

            // ⟶ und das File-Form-Field muss „file" heißen, nicht „audio"
            var fileContent = new ByteArrayContent(ms.ToArray());
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(request.AudioFile.ContentType);
            content.Add(fileContent, "file", request.AudioFile.FileName);

            // 3) Request absetzen
            var response = await _elevenlabsHttpClient.PostAsync("/v1/speech-to-text", content);

            // 4) Antwort-Body immer erst auslesen, dann EnsureSuccess oder eigenes Logging
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ElevenLabs Scribe API request failed. StatusCode={StatusCode} BodyLength={BodyLength}",
                    response.StatusCode,
                    body?.Length ?? 0);
                _logger.LogDebug("ElevenLabs error body prefix: {BodyPrefix}", LoggingSanitizer.Truncate(body, 500));
                throw new Exception($"Transkription fehlgeschlagen ({response.StatusCode}).");
            }

            // 5) JSON parsen und reines Text-Feld zurückgeben
            var result = JsonSerializer.Deserialize<ScribeResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var debugPayloadEnabled = _loggingOptions.DebugPayload.Enabled;
            var maxPayloadLength = _loggingOptions.DebugPayload.MaxLength;
            if (debugPayloadEnabled || _logger.IsEnabled(LogLevel.Debug))
            {
                var truncated = LoggingSanitizer.Truncate(body, maxPayloadLength);
                var suffix = body != null && body.Length > maxPayloadLength ? "…(truncated)" : string.Empty;

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("ElevenLabs response JSON (payload). Response={Response}{Suffix}", truncated, suffix);
                }
                else
                {
                    _logger.LogInformation("ElevenLabs response JSON (payload). Response={Response}{Suffix}", truncated, suffix);
                }
            }
            
            var transcript = result?.Text ?? string.Empty;
            _logger.LogInformation("ElevenLabs transcription finished. TranscriptLength={TranscriptLength} ElapsedMs={ElapsedMs}",
                LoggingSanitizer.SafeLength(transcript),
                sw.ElapsedMilliseconds);
            return transcript;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ElevenLabs audio transcription");
            throw;
        }
    }
}