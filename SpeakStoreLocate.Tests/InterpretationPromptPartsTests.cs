using SpeakStoreLocate.ApiService.Services.Interpretation;

namespace SpeakStoreLocate.Tests;

public class InterpretationPromptPartsTests
{
    [Fact]
    public void GetLocationsInformation_EmptyList_ProducesNoItemsAndNoTrailingComma()
    {
        // Arrange
        IInterpretationPromptParts parts = new InterpretationPromptParts();
        var locations = Array.Empty<string>();

        // Act
        var text = parts.GetLocationsInformationForImprovedLocationDetermination(locations);

        // Assert
        Assert.Contains("\"Lokationen\": [", text);
        // Keine Einträge
        Assert.DoesNotContain("\"Name\":", text);
        // Kein trailing comma vor ]
        Assert.DoesNotContain(",\n]\n}", text.Replace("\r\n", "\n"));
        Assert.EndsWith("besser zu bestimmen.", text.Trim());
    }

    [Fact]
    public void GetLocationsInformation_TwoLocations_ListedWithCommasBetweenButNotAtEnd()
    {
        // Arrange
        IInterpretationPromptParts parts = new InterpretationPromptParts();
        var locations = new[] { "Regal A", "Regal B" };

        // Act
        var text = parts.GetLocationsInformationForImprovedLocationDetermination(locations).Replace("\r\n", "\n");

        // Assert
        // Beide Einträge vorhanden
        Assert.Contains("{ \"Name\": \"Regal A\" }", text);
        Assert.Contains("{ \"Name\": \"Regal B\" }", text);
        // Komma nur zwischen den Einträgen
        var idxA = text.IndexOf("{ \"Name\": \"Regal A\" }");
        var idxComma = text.IndexOf(",\n", idxA, StringComparison.Ordinal);
        Assert.True(idxComma > idxA);
        // Kein Komma direkt vor dem schließenden ]
        Assert.DoesNotContain(",\n]\n}", text);
    }
}

