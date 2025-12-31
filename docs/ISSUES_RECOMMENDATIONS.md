# Issue-Pakete: Robustere STT + Interpretation + Storage

> Ziel: bessere Ergebnisqualität (weniger Parsingfehler, weniger falsche Aktionen), saubere Fallbacks, bessere Nachvollziehbarkeit.

---

## ISSUE 1 — `StorageCommand` & Prompt konsistent machen (Source/PUT)

**Problem**
- Prompt fordert `source` (bei `PUT` mandatory), aber `StorageCommand` hat kein `Source`. Das führt zu Informationsverlust und inkonsistentem Verhalten.

**Scope**
- `StorageCommand` um `source` ergänzen (nullable).
- Prompt/Examples so anpassen, dass sie exakt der Modellstruktur entsprechen.
- Repository-Handling für `PUT` prüfen (mindestens: Validierung/Logging; optional: Nutzung von `source` fürs Matching).

**Akzeptanzkriterien**
- API kann Commands mit `source` parsen, ohne Fehler.
- `PUT` Commands mit `source` werden korrekt verarbeitet (mindestens ohne Datenverlust).
- Unit Test(s): JSON Deserialize mit `source` klappt.

**Betroffene Stellen**
- SpeakStoreLocate.ApiService/Models/StorageCommand.cs
- SpeakStoreLocate.ApiService/Services/Interpretation/InterpretationPromptParts.cs
- SpeakStoreLocate.ApiService/Services/Storage/AwsStorageRepository.cs

**Aufwand**
- S: 1–2h

---

## ISSUE 2 — Robustere Interpretation: Structured Outputs/Schema statt „Bitte JSON“

**Problem**
- Aktuell: Prompt → Chat → `JsonSerializer.Deserialize`. Bei Codefences/Fehlformatierung/zusätzlichem Text bricht die Robustheit.

**Scope**
- Chat-Aufruf auf „strukturierte Ausgabe“ umstellen (JSON-Schema / tool-calling / strict response format; abhängig vom OpenAI .NET SDK).
- Serverseitige Validierung (Schema/Required fields) + 1 Retry mit „repair prompt“, falls Parsing/Validierung fehlschlägt.
- Logging verbessern: nur Prompt-Hash/Metadaten statt kompletten Prompt in Debug (PII/Noise).

**Akzeptanzkriterien**
- Interpretation liefert entweder valide `List<StorageCommand>` oder eine nachvollziehbare Fehlermeldung (ProblemDetails).
- Mindestens 1 automatisierter Test: „kaputte Modellantwort“ → Retry → ok / oder sauberer Fehler.

**Betroffene Stellen**
- SpeakStoreLocate.ApiService/Services/ChatCompletion/OpenAiChatCompletionService.cs
- SpeakStoreLocate.ApiService/Services/Interpretation/InterpretationService.cs

**Aufwand**
- M: 0.5–1 Tag

---

## ISSUE 3 — Location Canonicalization in Code (statt im Prompt)

**Problem**
- Duplikate/Abweichungen („Regal A“ vs „Regal-A“) werden dem LLM überlassen. Das ist nicht deterministisch und driftet.

**Scope**
- Deterministischer Matcher: normalize + string similarity (z.B. Levenshtein/Jaro-Winkler oder Token overlap) gegen bekannte Locations.
- LLM darf „raw destination“ liefern; API mappt auf kanonischen Wert.
- Optional: Konfigurierbarer Threshold; bei Unklarheit bleibt Location unverändert oder Rückfrage/No-Op.

**Akzeptanzkriterien**
- Neue Location wird nur angelegt, wenn kein ausreichend guter Match existiert.
- Tests: 5–10 Beispielpaare (Regal A / Regal-A / „regal a “) mappen identisch.

**Betroffene Stellen**
- SpeakStoreLocate.ApiService/Services/Storage/AwsStorageRepository.cs
- SpeakStoreLocate.ApiService/Utils/StringExtensions.cs (oder neuer util)

**Aufwand**
- M: 0.5 Tag

---

## ISSUE 4 — `DELETE` ohne Destination unterstützen (natürliche Sprache)

**Problem**
- `DELETE` wird aktuell übersprungen, wenn `Destination` leer ist. In der Praxis sagt man oft nur „nimm X raus“.

**Scope**
- Repository-Logik: `DELETE` auch ohne Destination erlaubt (nur via ItemName suchen).
- Optional: wenn mehrere Treffer → definierte Strategie (z.B. „most recent“ oder Fehler zurückgeben).

**Akzeptanzkriterien**
- „Entnimm die Lampe“ funktioniert ohne explizite Location.
- Fehlerfall „mehrdeutig“ ist sauber definiert (400 mit Message).

**Betroffene Stellen**
- SpeakStoreLocate.ApiService/Services/Storage/AwsStorageRepository.cs

**Aufwand**
- S: 1–3h

---

## ISSUE 5 — TranscriptionResult einführen (statt nur `string`)

**Problem**
- Nur `string` verhindert Quality Gates, Provider-Vergleiche, Sprache/Confidence-Checks und Debugging.

**Scope**
- Neues Modell `TranscriptionResult` (Text, Language, Provider, DurationMs, Segments optional).
- `ITranscriptionService` auf `Task<TranscriptionResult>` umstellen.
- Minimal: bestehende Implementierungen setzen nur `Text` + `Provider`.

**Akzeptanzkriterien**
- API-Endpunkt nutzt `TranscriptionResult.Text`.
- Keine Breaking Changes im Client-API Contract (Controller response bleibt gleich).
- Tests: Service returns valid result object.

**Betroffene Stellen**
- SpeakStoreLocate.ApiService/Services/Transcription/ITranscriptionService.cs
- SpeakStoreLocate.ApiService/Services/Transcription/*TranscriptionService.cs
- SpeakStoreLocate.ApiService/Controllers/StorageController.cs

**Aufwand**
- M: 0.5–1 Tag

---

## ISSUE 6 — Provider-Orchestrator mit Fallbacks (STT)

**Problem**
- Single-Provider erhöht Ausfall- und Qualitätsrisiko.

**Scope**
- Orchestrator `TranscriptionOrchestrator`: Primary + Fallback chain.
- Heuristiken: empty text, very short text, exception, timeout → fallback.
- Telemetrie/Logs: Provider, Dauer, Fallback-Grund.

**Akzeptanzkriterien**
- Bei Provider-Fehler wird automatisch auf Fallback gewechselt.
- Logs zeigen eindeutig Provider + Fallback reason.

**Betroffene Stellen**
- SpeakStoreLocate.ApiService/Services/Transcription/*
- SpeakStoreLocate.ApiService/Extensions/ApplicationServicesExtensions.cs

**Aufwand**
- M: 0.5–1 Tag

---

## ISSUE 7 — Selfhosted Whisper (faster-whisper) Sidecar + .NET Client

**Problem**
- Deepgram kann (je nach Plan) unnötig teuer sein; selfhosted ist bei 30min/Monat sehr attraktiv.

**Scope**
- Neuer Service (Docker) `whisper-sidecar`: FastAPI endpoint `POST /transcribe` (multipart file).
- Sidecar nutzt `faster-whisper` (default model `small`).
- .NET: neue `WhisperTranscriptionService` Implementierung ruft Sidecar via `HttpClient` an.
- Konfig: `Whisper:BaseUrl`, `Whisper:Model`, `Whisper:Language`.

**Akzeptanzkriterien**
- Lokal via Docker: UploadAudio transkribiert erfolgreich.
- Dokumentation: Start/Run-Anleitung in docs.

**Betroffene Stellen**
- Neuer Ordner z.B. `whisper/` (Dockerfile, app.py)
- SpeakStoreLocate.ApiService/Services/Transcription/WhisperTranscriptionService.cs (neu)
- appsettings.*.json

**Aufwand**
- L: 1–2 Tage (je nach Docker/Deploy)

---

## ISSUE 8 — Observability: Correlation IDs + strukturierte Logs für Pipeline

**Problem**
- Debugging ist schwer, wenn STT/LLM/DB zusammenhängen.

**Scope**
- CorrelationId pro Request (Middleware oder Serilog enricher).
- Log Events: `TranscriptionCompleted`, `InterpretationCompleted`, `CommandsValidated`, `ActionsPerformed`.
- Optional: OpenTelemetry spans.

**Akzeptanzkriterien**
- Ein Request ist in Logs eindeutig end-to-end nachvollziehbar.

**Betroffene Stellen**
- SpeakStoreLocate.ApiService/Program.cs / Middleware / SerilogExtensions
- Controller/Services

**Aufwand**
- M: 0.5–1 Tag

---

## ISSUE 9 — Safety/Idempotenz: Duplicate Uploads & “No-Op” Regeln

**Problem**
- Gleiche Sprachnachricht mehrfach senden kann doppelte Einlagerung erzeugen.

**Scope**
- Idempotency-Key (z.B. hash aus Audio oder request header) + „already processed“ Schutz.
- Rules: Wenn Command unvollständig/unsicher → No-Op oder 400.

**Akzeptanzkriterien**
- Wiederholtes Senden führt nicht zu doppelten Writes.

**Aufwand**
- L: 1–2 Tage (je nach Persistenz)

---

## Empfohlene Reihenfolge
1) ISSUE 1 (Source/Schema konsistent)
2) ISSUE 2 (Structured outputs + Validierung)
3) ISSUE 4 (DELETE ohne Destination)
4) ISSUE 3 (Location canonicalization)
5) ISSUE 5 (TranscriptionResult)
6) ISSUE 6 (Fallback orchestrator)
7) ISSUE 7 (Selfhosted Whisper)
8) ISSUE 8 (Observability)
9) ISSUE 9 (Idempotenz)
