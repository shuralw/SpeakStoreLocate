using SpeakStoreLocate.ApiService.Services.Interpretation;
using Microsoft.Extensions.Logging.Abstractions;

namespace SpeakStoreLocate.Tests;

public class InterpretationPromptBuilderTests
{
    [Fact]
    public void BuildPrompt_Composes_System_Locations_And_Transcript()
    {
        // Arrange
        IInterpretationPromptParts parts = new InterpretationPromptParts();
        IInterpretationPromptBuilder builder = new InterpretationPromptBuilder(parts, NullLogger<InterpretationPromptBuilder>.Instance);
        var transcript = "Packe die Lampe nach Regal B.";
        var locations = new[] { "Regal A", "Regal B" };

        // Act
        var prompt = builder.BuildPrompt(transcript, locations).Replace("\r\n", "\n");

        // Assert
        Assert.Contains("Lampe nach Regal B", prompt);
        Assert.Contains("\"Lokationen\": [", prompt);
        Assert.Contains("{ \"Name\": \"Regal A\" }", prompt);
        Assert.Contains("{ \"Name\": \"Regal B\" }", prompt);
        // System-Teil sollte an Anfang stehen
        var systemIdx = prompt.IndexOf("Du bist ein Parser", StringComparison.OrdinalIgnoreCase);
        var locationsIdx = prompt.IndexOf("\"Lokationen\"", StringComparison.Ordinal);
        var transcriptIdx = prompt.IndexOf("Transkript (de-DE):", StringComparison.Ordinal);
        Assert.True(systemIdx >= 0 && systemIdx < locationsIdx && locationsIdx < transcriptIdx);
    }
}