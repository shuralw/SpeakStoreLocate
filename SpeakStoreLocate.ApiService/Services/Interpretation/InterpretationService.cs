using System.Text.Json;
using SpeakStoreLocate.ApiService.Models;
using SpeakStoreLocate.ApiService.Services.ChatCompletion;

namespace SpeakStoreLocate.ApiService.Services.Interpretation;

class InterpretationService(ILogger<InterpretationService> _logger, IChatCompletionService _chatCompletionService)
    : IInterpretationService
{
    private string x =
        @"Du bist ein Kommissionierungssystem für ein Lager, das für den User herausfindet, wo sich Objekte befinden. 
            Du erhältst ein JSON und sollst für jedes JSON-Element herausfinden, in welcher Lokation es sich befindet.
            Erzeuge **striktes** JSON (keine Freitext‑Antwort!), und zwar ein Array von Objekten mit genau diesen zwei Feldern:
            • itemName      – der Name des gesuchten Objekts.  
            • source        – die Lokation, an der sich das Objekt befindet.
          
            Das Lager enthält die folgenden Lokationen mit deren respektiven Objekten:
            {
                ""Lokationen"": 
                [ 
                     {
                        ""Name"": ""Farbkiste 1"" 
                        ""Inhalte"": [
                            Objekt: { ""Name"": ""Farbrolle"" },
                            Objekt: { ""Name"": ""Farbwalze"" },
                            Objekt: { ""Name"": ""grüne Wandfarbe"" },
                            Objekt: { ""Name"": ""graue Wandfarbe"" },
                        ]
                    },
                    {
                        ""Name"": ""Farbkiste 2"" 
                        ""Inhalte"": [
                            Objekt: { ""Name"": ""großer Pinsel in blau"" },
                            Objekt: { ""Name"": ""kleiner Pinsel in grün"" },
                        ]
                    }
                ]
            }}";

    readonly string systemPrompt = @"
            Du bist ein Parser, der aus einem deutschsprachigen Transkript alle Lager‑Aktionen extrahiert.  
            Erzeuge **striktes** JSON (keine Freitext‑Antwort!), und zwar ein Array von Objekten mit genau diesen vier Feldern:

            • method        – eine der Zeichenketten ""GET"", ""DELETE"", ""POST"" oder ""PUT""  
            • count         – eine Ganzzahl (1,2,3…)  
            • itemName      – der exakte Artikelname (inkl. Groß‑/Kleinschreibung wie im Transkript)  
            • source        – die Quelllokation (optional, nur bei PUT mandatory) 
            • destination   – die Ziellokation (optional - bleibt leer bei GET)  

            **WICHTIG: Edge Cases behandeln:**
            - Wenn der Text keine erkennbaren Lager-Aktionen enthält, gib ein leeres Array zurück: []
            - Wenn der Text zu kurz oder bedeutungslos ist, gib ein leeres Array zurück: []
            - Ignoriere reine Füllwörter, Pausen, oder unverständliche Laute

            **Regeln für die Methodenwahl:**  
            1. **PUT**  nur wenn **im selben Satz**  
                - eine **Quelllokation** (z.B. „von Regal A“)  
                - **und** eine **Ziellokation** (z.B. „nach Regal B“) explizit genannt werden.  
            2. **DELETE** wenn der User etwas „entnimmt“, „ausschüttet“, „herausnimmt“ o.Ä.  
            3. **GET**    wenn der User nach dem Ort fragt („wo ist…“, „suche…“).    
            4. **POST**  in **allen anderen Fällen**, also  
                - „einlagern“, „ablegen“, „in … tun“, „hängen“, „befestigen“, „stellen“,  
                - oder wenn nur eine Lokation angegeben ist ohne Quelle.  

            **Weiteres Optimierungspotenzial:**  
                - Füge bei PUT‑Befehlen das Feld `""source""` hinzu, um die Quell‑Lokation zu speichern.  
                - Gib immer `""count"": 1`, wenn keine Zahl genannt wird.  
                - Ersetze ausgeschriebene Zahlen („drei“) durch Ziffern (3).  
                - Normalisiere Leer‑ und Sonderzeichen (Trim, keine führenden/trailenden Leerzeichen).  
                - Wenn ein Satz kein valides Kommando enthält, ignoriere ihn schlicht.  
                - Bei Suchen soll der Itemname gesetzt werden, der Ort wird allerdings nicht identifiziert und bleibt leer.
                - Falls du offensichtliche Rechtschreibfehler erkennst, die der Transkriptor erzeugt haben könnte, korrigiere diese. Das kommt aber relativ selten vor.
                - Filler‑Wörter: Entferne Artikel (der, die, das) und Füllwörter, aber nur soweit, dass der eigentliche Artikelname klar bleibt.    

            **Beispiel-Ausgaben:**
            
            Für: ""Und das Fahrrad wird an der Wand aufgehangen.""
                [
                    {
                        ""method"":   ""POST"",
                        ""count"":    1,
                        ""itemName"": ""Fahrrad"",
                        ""destination"": ""Wand""
                    }
                ]
            
            Für: ""Verschiebe die Lampe von Regal A nach Regal B.""
                [
                    {
                        ""method"":   ""PUT"",
                        ""count"":    1,
                        ""itemName"": ""Lampe"",
                        ""source"":   ""Regal A"",
                        ""destination"": ""Regal B""
                    }
                ]

            Für bedeutungslose Texte: []

            **WICHTIG:** Wenn du unsicher bist oder der Text keine klaren Lager-Aktionen enthält, gib lieber ein leeres Array [] zurück als ungültige Kommandos zu erfinden!";

    public async Task<List<StorageCommand>> InterpretGeschwafelToStructuredCommands(string transcriptedText)
    {
        // Edge Case 1: Leere oder bedeutungslose Transkription
        if (string.IsNullOrWhiteSpace(transcriptedText))
        {
            _logger.LogWarning("Empty or null transcription received");
            throw new ArgumentException("Transcription is empty or contains no meaningful content");
        }

        // Kompletten Prompt zusammenfügen und absenden
        string fullPrompt = $"System: {systemPrompt}\n" +
                            $"User: {transcriptedText}";

        _logger.LogDebug("Vollständiger Prompt: {fullPrompt}", fullPrompt);

        string chatResponse = await _chatCompletionService.CompleteChat(fullPrompt);

        // Edge Case 4: Überprüfe ob die AI-Antwort gültiges JSON ist
        List<StorageCommand> commands;
        try
        {
            commands = JsonSerializer.Deserialize<List<StorageCommand>>(
                chatResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<StorageCommand>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response as JSON: {Response}", chatResponse);
            throw new InvalidOperationException("AI service returned invalid response format");
        }

        // Edge Case 5: Keine gültigen Kommandos gefunden
        if (commands.Count == 0)
        {
            _logger.LogWarning("No storage commands found in transcription: '{Text}'", transcriptedText);
            throw new ArgumentException("No valid storage commands found in the provided text");
        }

        // Edge Case 6: Validiere dass die Kommandos gültige Daten enthalten
        var validCommands = commands.Where(cmd =>
            !string.IsNullOrWhiteSpace(cmd.ItemName) &&
            !string.IsNullOrWhiteSpace(cmd.Method) &&
            cmd.Count > 0
        ).ToList();

        if (validCommands.Count == 0)
        {
            _logger.LogWarning("All parsed commands are invalid: {Commands}", JsonSerializer.Serialize(commands));
            throw new ArgumentException("All parsed commands contain invalid data");
        }

        if (validCommands.Count != commands.Count)
        {
            _logger.LogInformation("Filtered out {InvalidCount} invalid commands from {TotalCount} total commands",
                commands.Count - validCommands.Count, commands.Count);
        }

        return validCommands;
    }
}