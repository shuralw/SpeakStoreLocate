using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;

namespace SpeakStoreLocate.ApiService;

class InterpretationService(ILogger<InterpretationService> _logger, IChatCompletionService _chatCompletionService) : IInterpretationService
{
    readonly string systemPrompt = @"
            Du bist ein Parser, der aus einem deutschsprachigen Transkript alle Lager‑Aktionen extrahiert.  
            Erzeuge **striktes** JSON (keine Freitext‑Antwort!), und zwar ein Array von Objekten mit genau diesen vier Feldern:

            • method        – eine der Zeichenketten ""GET"", ""DELETE"", ""POST"" oder ""PUT""  
            • count         – eine Ganzzahl (1,2,3…)  
            • itemName      – der exakte Artikelname (inkl. Groß‑/Kleinschreibung wie im Transkript)  
            • source        – die Quelllokation (optional, nur bei PUT mandatory) 
            • destination   – die Ziellokation (optional - bleibt leer bei GET)  

            **Regeln für die Methodenwahl:**  
            1. **PUT**  nur wenn **im selben Satz**  
                - eine **Quelllokation** (z.B. „von Regal A“)  
                - **und** eine **Ziellokation** (z.B. „nach Regal B“) explizit genannt werden.  
            2. **DELETE** wenn der User etwas „entnimmt“, „ausschüttet“, „herausnimmt“ o. Ä.  
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

            **Beispiel-Ausgabe** für deinen Text:
                >„Und das Fahrrad wird an der Wand aufgehangen.“
                [
                    {
                        ""method"":   ""POST"",
                        ""count"":    1,
                        ""itemName"": ""Fahrrad"",
                        ""destination"": ""Wand""
                    }
                ]
            Und für
                >„Verschiebe die Lampe von Regal A nach Regal B.“
                [
                    {
                        ""method"":   ""PUT"",
                        ""count"":    1,
                        ""itemName"": ""Lampe"",
                        ""source"":   ""Regal A"",
                        ""destination"": ""Regal B""
                    }
                ]";

    public async Task<List<StorageCommand>> InterpretGeschwafelToStructuredCommands(string transcriptedText)
    {
        // Kompletten Prompt zusammenfügen und absenden
        string fullPrompt = $"System: {systemPrompt}\n" +
                            $"User: {transcriptedText}";

        _logger.LogDebug("Vollständiger Prompt: {fullPrompt}", fullPrompt);

        string chatResponse = await _chatCompletionService.CompleteChat(fullPrompt);

        // 6) In List<StorageCommand> deserialisieren
        var commands = JsonSerializer.Deserialize<List<StorageCommand>>(
            chatResponse,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? new List<StorageCommand>();
        return commands;
    }
}