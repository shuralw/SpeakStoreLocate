namespace SpeakStoreLocate.ApiService.Services.Interpretation;

public interface IInterpretationPromptParts
{
    string GetSystemPrompt();
    string GetLocationsInformationForImprovedLocationDetermination(IEnumerable<string> existingLocations);
}

public class InterpretationPromptParts : IInterpretationPromptParts
{
    private const string SystemPrompt = @"
            Du bist ein Parser, der aus einem deutschsprachigen Transkript alle Lager‑Aktionen extrahiert.  
            Erzeuge **striktes** JSON (keine Freitext‑Antwort!), und zwar ein Array von Objekten mit genau diesen vier Feldern:

            • method        – eine der Zeichenketten ""GET"", ""DELETE"", ""POST"" oder ""PUT""  
            • count         – eine Ganzzahl (1,2,3…); setze auf die im Transkript genannte Anzahl, oder 1 wenn keine Anzahl genannt wird  
            • itemName      – der Artikelname; wenn im Transkript eine Anzahl genannt wird, formatiere als ""<count>x <itemName>"" (z.B. ""3x Schraube""); wenn keine Anzahl genannt wird, verwende nur den Artikelnamen ohne Mengenpräfix  
            • source        – die Quelllokation (optional, nur bei PUT mandatory) 
            • destination   – die Ziellokation (optional - bleibt leer bei GET)  

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
                - Wenn im Transkript eine Anzahl genannt wird, füge diese als Präfix zum `""itemName""` hinzu (Format: ""<count>x <itemName>""); wenn keine Anzahl genannt wird, verwende den Artikelnamen ohne Mengenpräfix.  
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
                ]
            Und für
                >„Lege drei Schrauben in Regal A.“
                [
                    {
                        ""method"":   ""POST"",
                        ""count"":    3,
                        ""itemName"": ""3x Schraube"",
                        ""destination"": ""Regal A""
                    }
                ]";

    public string GetSystemPrompt() => SystemPrompt;

    public string GetLocationsInformationForImprovedLocationDetermination(IEnumerable<string> existingLocations)
    {
        var locations = existingLocations?.ToArray() ?? Array.Empty<string>();

        var sb = new System.Text.StringBuilder();
        sb.Append("Das Lager enthält bereits die folgenden Lokationen:\n{\n\"Lokationen\": [\n");

        for (int i = 0; i < locations.Length; i++)
        {
            var loc = locations[i];
            sb.Append("    { \"Name\": \"")
              .Append(loc)
              .Append("\" }");
            if (i < locations.Length - 1)
                sb.Append(",");
            sb.Append("\n");
        }

        sb.Append("]\n}. Nutze diese Information, um die Quelle und das Ziel von Lagerbewegungen besser zu bestimmen und dadurch duplikate Lokationen aufgrund von geringfügigen Abweichungen zu vermeiden.");
        return sb.ToString();
    }
}
