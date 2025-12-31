# STT Test Assets

Lege hier eigene (selbst aufgenommene) Audiodateien + Referenztranskripte ab, um die STT-Qualität empirisch zu testen.

## Format

Pro Testfall:
- Eine Audiodatei, z.B. `case-001.webm` (idealerweise direkt aus dem Browser/Client aufgenommen).
- Eine Textdatei mit identischem Basisnamen, z.B. `case-001.txt`, mit dem *erwarteten* Transkript.

Beispiel:
- `Assets/case-001.webm`
- `Assets/case-001.txt`

## Ausführung

Die Tests rufen Deepgram live auf (Integrationstest). Setze dazu:

- `DEEPGRAM_API_KEY` (required)

Optional:
- `STT_WER_THRESHOLD` (default: 0.20)

Ohne `DEEPGRAM_API_KEY` werden die Tests automatisch übersprungen.

## Hinweise

- Committe nur Audio, das du selbst aufgenommen hast (keine fremden Inhalte).
- Für stabile Ergebnisse: immer gleiches Mikro/Umgebung, ähnlich lange Sätze.
