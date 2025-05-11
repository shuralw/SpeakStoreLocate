using System.Net.Http.Headers;
using System.Text.Json;

namespace SpeakStoreLocate.ApiService;

public class ElevenlabsTranscriptionService : ITranscriptionService
{
    private readonly ILogger<ElevenlabsTranscriptionService> _logger;
    private readonly HttpClient _elevenlabsHttpClient;

    public ElevenlabsTranscriptionService(
        IConfiguration configuration,
        ILogger<ElevenlabsTranscriptionService> logger)
    {
        _logger = logger;
        string? elevenLabsApiKey = configuration["ELEVENLABS_API_KEY"];
        _elevenlabsHttpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.elevenlabs.io")
        };
        // direkt nach Erzeugung des HttpClient
        _elevenlabsHttpClient.DefaultRequestHeaders.Clear();
        _elevenlabsHttpClient.DefaultRequestHeaders.Add("xi-api-key", elevenLabsApiKey);
    }

    public async Task<string> TranscriptAudioAsync(AudioUploadRequest request)
    {
        // 1) Datei in MemoryStream laden
        using var ms = new MemoryStream();
        await request.AudioFile.CopyToAsync(ms);
        ms.Position = 0;

        // 2) Multipart/FormData-Content aufbauen
        using var content = new MultipartFormDataContent();
        // ⟶ unbedingt model_id mitsenden
        content.Add(new StringContent("scribe_v1"), "model_id");
        // ⟶ optional Sprache angeben
        content.Add(new StringContent("de"), "language_code");

        // ⟶ und das File-Form-Field muss „file“ heißen, nicht „audio“
        var fileContent = new ByteArrayContent(ms.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(request.AudioFile.ContentType);
        content.Add(fileContent, "file", request.AudioFile.FileName);

        // 3) Request absetzen
        var response = await _elevenlabsHttpClient.PostAsync("/v1/speech-to-text", content);

        // 4) Antwort-Body immer erst auslesen, dann EnsureSuccess oder eigenes Logging
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("ElevenLabs Scribe API Fehler {StatusCode}: {Body}",
                response.StatusCode, body);
            throw new Exception($"Transkription fehlgeschlagen ({response.StatusCode}): {body}");
        }

        // 5) JSON parsen und reines Text-Feld zurückgeben
        var result = JsonSerializer.Deserialize<ScribeResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return result?.Text ?? string.Empty;
    }
}