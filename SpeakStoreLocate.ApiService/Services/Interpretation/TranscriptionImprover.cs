using SpeakStoreLocate.ApiService.Services.ChatCompletion;

namespace SpeakStoreLocate.ApiService.Services.Interpretation;

internal class TranscriptionImprover(ILogger<TranscriptionImprover> _logger, IChatCompletionService _chatCompletionService) : ITranscriptionImprover
{
    private const string systemPrompt = @"
        Du bist ein spezialisierter Textverbesserer für Lagerverwaltungs-Transkripte.
        Deine Aufgabe: Roh-Transkripte in klare, wörtlich treue, strukturierte Lager-/Kommissionier-Kommandos umformen.

        GRUNDLEGENDE PRINZIPIEN (NICHT verletzen):
        1. Keine Halluzinationen oder inhaltlichen Ergänzungen. Nichts erfinden was nicht gesagt wurde.
        2. Keine Bedeutungs-Uminterpretation von Markennamen oder Artikeln:
           - 'Duplo' bleibt exakt 'Duplo' (NICHT automatisch 'Duplo-Steine').
           - Markennamen / Eigenbezeichnungen unverändert lassen.
        3. Füllphrasen, Zögerlaute und irrelevante Sprechanteile entfernen (z.B. 'äh', 'öhm', etc. wenn nicht wirklich ein Auftrag).
        4. Mehrere Aktionen sauber separieren: Jede Aktion = eigener Satz. Keine Aktionen zusammenziehen.
        5. Wenn mehrere unterschiedliche Artikel in dieselbe Lokation sollen und dies erkennbar ist, gib zwei getrennte Sätze mit identischer Lokationsangabe aus.
        6. Wenn 'getrennt' oder sinngemäß getrennte Aufbewahrung erwähnt wird, dies klar formulieren (aber nur wenn wirklich gesagt / impliziert – nicht erfinden).
        7. Mengen/Zahlen normalisieren (""zwei"" -> ""2""), sofern eindeutig.
        8. Unklare oder unvollständige Lokation NICHT raten oder erweitern.
        9. Zeitliche oder nicht-operative Aussagen weglassen, falls sie keine Lageraktion sind.
       10. Reihenfolge der Aktionen beibehalten.

        TRANSKRIPTIONSFEHLER korrigieren (nur offensichtliche):
           Regel -> Regal, Karsten -> Kasten, Fag -> Fach.

        AUSGABEFORMAT:
        - Eine oder mehrere klar formulierte deutsche Anweisungssätze.
        - Keine Aufzählungszeichen, kein JSON, keine zusätzlichen Erklärungen.
        - Nur die bereinigten Kommandos, sonst nichts.

        BEISPIELE:
        Eingabe: 'Leg das ahm Fahrrad ins Regel zwei'
        Ausgabe: 'Lege das Fahrrad in Regal 2.'

        Eingabe: 'Und dann Duplo und die Kabelbinder in die oberste Schublade'
        Ausgabe: 'Lege Duplo in die oberste Schublade.' 'Lege die Kabelbinder in die oberste Schublade.'

        Eingabe: 'Ich würde erst mal volle Kanne gar nichts tun und dann hab ich hier noch Duplo rumliegen und dieses Duplo das würde ich ganz gerne mit den Kabelbindern zusammen jeweils getrennt in die oberste Schublade reintun'
        Ausgabe: 'Lege Duplo getrennt in die oberste Schublade.' 'Lege die Kabelbinder getrennt in die oberste Schublade.'

        Wenn kein valides Kommando vorhanden ist, liefere den minimal bereinigten sinnvollen Rest.
        Gib ausschließlich die finalen bereinigten Sätze zurück.";
    public async Task<string> ImproveTranscriptedText(string transcriptedText)
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

        return chatResponse;
    }
}