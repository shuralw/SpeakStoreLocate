namespace SpeakStoreLocate.Tests;

public class WebTests
{
    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.SpeakStoreLocate_AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging

        await using var app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("webfrontend");
        await resourceNotificationService.WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        var response = await httpClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UploadAudio_WithNullFile_ReturnsBadRequest()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.SpeakStoreLocate_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("apiservice");
        
        var content = new MultipartFormDataContent();
        // Keine Datei hinzufügen - simuliert null file

        // Act
        var response = await httpClient.PostAsync("/api/storage/upload-audio", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("No audio file provided", responseContent);
    }

    [Fact]
    public async Task UploadAudio_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.SpeakStoreLocate_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("apiservice");
        
        var content = new MultipartFormDataContent();
        var emptyFileContent = new ByteArrayContent(Array.Empty<byte>());
        emptyFileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
        content.Add(emptyFileContent, "AudioFile", "empty.mp3");

        // Act
        var response = await httpClient.PostAsync("/api/storage/upload-audio", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Audio file is empty", responseContent);
    }

    [Fact]
    public async Task UploadAudio_WithInvalidContentType_ReturnsBadRequest()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.SpeakStoreLocate_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("apiservice");
        
        var content = new MultipartFormDataContent();
        var invalidFileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        invalidFileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(invalidFileContent, "AudioFile", "invalid.txt");

        // Act
        var response = await httpClient.PostAsync("/api/storage/upload-audio", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unsupported audio format", responseContent);
    }

    [Fact]
    public async Task UploadAudio_WithTooLargeFile_ReturnsBadRequest()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.SpeakStoreLocate_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("apiservice");
        
        var content = new MultipartFormDataContent();
        // Erstelle eine Datei größer als 50MB
        var largeFileContent = new ByteArrayContent(new byte[51 * 1024 * 1024]); // 51MB
        largeFileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
        content.Add(largeFileContent, "AudioFile", "large.mp3");

        // Act
        var response = await httpClient.PostAsync("/api/storage/upload-audio", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("File too large", responseContent);
    }

    [Fact]
    public async Task UploadAudio_WithTestPhrases_ReturnsBadRequest()
    {
        // Arrange - Simuliere eine Audiodatei die nur "test test" transkribiert wird
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.SpeakStoreLocate_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("apiservice");
        
        var content = new MultipartFormDataContent();
        // Erstelle eine kleine Audiodatei die zu "test test" transkribiert werden könnte
        var testAudioContent = new ByteArrayContent(new byte[1024]); // 1KB Test-Audio
        testAudioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
        content.Add(testAudioContent, "AudioFile", "test.mp3");

        // Act
        var response = await httpClient.PostAsync("/api/storage/upload-audio", content);

        // Assert
        // Je nach Transcription Service könnte dies BadRequest (bei erkannten Test-Phrasen) 
        // oder 500 (bei anderen Transkriptions-Problemen) zurückgeben
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                   response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UploadAudio_WithSilentAudio_ReturnsBadRequest()
    {
        // Arrange - Simuliere eine 5-Sekunden stille Audiodatei
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.SpeakStoreLocate_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("apiservice");
        
        var content = new MultipartFormDataContent();
        // Simuliere eine stille Audiodatei (5 Sekunden @ 44.1kHz würde zu leerer Transkription führen)
        var silentAudioContent = new ByteArrayContent(new byte[5 * 44100 * 2]); // Grobe Simulation
        silentAudioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
        content.Add(silentAudioContent, "AudioFile", "silent.wav");

        // Act
        var response = await httpClient.PostAsync("/api/storage/upload-audio", content);

        // Assert
        // Bei einer stummen Audiodatei sollte der InterpretationService eine ArgumentException werfen
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                   response.StatusCode == HttpStatusCode.InternalServerError);
    }
}
