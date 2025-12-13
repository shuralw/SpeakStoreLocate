# PR Preview Environments - Dokumentation

Diese Dokumentation beschreibt die automatisierten Preview-Umgebungen für Feature-Branches, die mit Fly.io umgesetzt wurden.

## Überblick

Für jeden Pull Request wird automatisch eine isolierte Preview-Umgebung erstellt, die es Reviewern ermöglicht, Änderungen live zu testen, bevor sie in den `master`-Branch gemergt werden.

## Funktionsweise

### Automatisches Deployment

1. **PR wird geöffnet oder aktualisiert**: 
   - Eine neue Preview-Umgebung wird automatisch erstellt
   - Die App wird mit dem Namen `speakstorelocate-pr-{PR_NUMBER}` deployed
   - Ein Kommentar mit der URL zur Preview-Umgebung wird im PR hinzugefügt

2. **PR wird aktualisiert** (neue Commits):
   - Die Preview-Umgebung wird automatisch neu deployed
   - Der Kommentar im PR wird mit dem aktuellen Zeitstempel aktualisiert

3. **PR wird geschlossen oder gemergt**:
   - Die Preview-Umgebung wird automatisch gelöscht
   - Ein Kommentar bestätigt die Löschung

### URL-Schema

Jede Preview-Umgebung ist unter einer eindeutigen URL erreichbar:
```
https://speakstorelocate-pr-{PR_NUMBER}.fly.dev
```

Beispiele:
- PR #42: `https://speakstorelocate-pr-42.fly.dev`
- PR #123: `https://speakstorelocate-pr-123.fly.dev`

## Einrichtung

### Voraussetzungen

1. **Fly.io Account**: 
   - Erstellen Sie einen Account auf [fly.io](https://fly.io/app/sign-up)
   - Erstellen Sie eine Organisation (oder verwenden Sie die persönliche Organisation)

2. **Fly.io API Token**:
   ```bash
   # Token generieren
   flyctl auth token
   ```

3. **GitHub Repository Secrets**:
   Fügen Sie folgende Secrets in den Repository-Einstellungen hinzu:
   - `FLY_API_TOKEN`: Das API Token von Fly.io
   - `FLY_ORG`: Der Name Ihrer Fly.io Organisation (z.B. "personal" oder Ihre Org-Name)

### Secrets einrichten

1. Navigieren Sie zu: `Settings` → `Secrets and variables` → `Actions`
2. Klicken Sie auf `New repository secret`
3. Fügen Sie folgende Secrets hinzu:

   | Name | Wert | Beschreibung |
   |------|------|--------------|
   | `FLY_API_TOKEN` | Ihr Fly.io API Token | Token für Fly.io API-Zugriff |
   | `FLY_ORG` | Ihr Fly.io Org-Name | Organisation für App-Erstellung |

### Fly.io CLI Installation (Optional)

Für lokale Tests können Sie die Fly.io CLI installieren:

```bash
# macOS/Linux
curl -L https://fly.io/install.sh | sh

# Windows (PowerShell)
iwr https://fly.io/install.ps1 -useb | iex
```

## Konfiguration

### fly.toml

Die Hauptkonfigurationsdatei für Fly.io befindet sich im Repository-Root:

```toml
app = "speakstorelocate"
primary_region = "fra"  # Frankfurt

[build]
  dockerfile = "SpeakStoreLocate.ApiService/dockerfile"

[env]
  ASPNETCORE_ENVIRONMENT = "Production"
  ASPNETCORE_URLS = "http://+:8080"

[http_service]
  internal_port = 8080
  force_https = true
  auto_stop_machines = true
  auto_start_machines = true
  min_machines_running = 0
```

### GitHub Workflow

Die Workflow-Datei `.github/workflows/pr-preview.yml` steuert die automatischen Deployments:

- **Trigger**: Pull Request Events (opened, synchronize, reopened, closed)
- **Deploy Job**: Erstellt/aktualisiert die Preview-Umgebung
- **Cleanup Job**: Löscht die Preview-Umgebung beim Schließen

## Verwendung

### Für Entwickler

1. **Branch erstellen**:
   ```bash
   git checkout -b feature/my-new-feature
   ```

2. **Änderungen committen und pushen**:
   ```bash
   git add .
   git commit -m "Add new feature"
   git push origin feature/my-new-feature
   ```

3. **Pull Request erstellen**:
   - Erstellen Sie einen PR auf GitHub
   - Der Preview-Deployment-Workflow startet automatisch
   - Nach 3-5 Minuten erscheint ein Kommentar mit der Preview-URL

### Für Reviewer

1. **Preview-URL finden**:
   - Öffnen Sie den Pull Request
   - Suchen Sie nach dem Kommentar "🚀 Preview Environment Deployed"
   - Klicken Sie auf die bereitgestellte URL

2. **Änderungen testen**:
   - Die Preview-Umgebung enthält die aktuellsten Änderungen aus dem PR-Branch
   - Testen Sie die neue Funktionalität
   - Geben Sie Feedback im PR

3. **Automatische Updates**:
   - Wenn der Entwickler neue Commits pusht, wird die Preview-Umgebung automatisch aktualisiert
   - Der Zeitstempel im Kommentar zeigt die letzte Aktualisierung

## Kosten und Ressourcen

### Fly.io Kostenmodell

- **Free Tier**: 
  - 3 shared-cpu-1x VMs mit 256MB RAM (kostenlos)
  - Ideal für Preview-Umgebungen
  
- **Auto-Stop**: 
  - VMs werden bei Inaktivität automatisch gestoppt
  - Keine Kosten bei gestoppten VMs
  - Automatischer Start bei Zugriff

### Ressourcen pro Preview

Jede Preview-Umgebung verwendet:
- **CPU**: 1 shared CPU
- **RAM**: 1GB
- **Region**: Frankfurt (fra)
- **Auto-Stop**: Aktiviert (spart Kosten)

## Troubleshooting

### Problem: Deployment schlägt fehl

**Lösung**:
1. Überprüfen Sie die GitHub Actions Logs
2. Stellen Sie sicher, dass `FLY_API_TOKEN` und `FLY_ORG` korrekt gesetzt sind
3. Überprüfen Sie, ob das Dockerfile korrekt ist

### Problem: App startet nicht

**Lösung**:
1. Überprüfen Sie die Fly.io Logs:
   ```bash
   flyctl logs --app speakstorelocate-pr-{PR_NUMBER}
   ```
2. Prüfen Sie die Umgebungsvariablen in `fly.toml`
3. Stellen Sie sicher, dass Port 8080 korrekt exponiert wird

### Problem: Alte Preview-Umgebungen nicht gelöscht

**Lösung**:
1. Manuelle Löschung über CLI:
   ```bash
   flyctl apps destroy speakstorelocate-pr-{PR_NUMBER} --yes
   ```
2. Oder über das Fly.io Dashboard

### Problem: Zu viele Apps erreicht Free Tier Limit

**Lösung**:
1. Löschen Sie alte, nicht mehr benötigte Preview-Apps:
   ```bash
   flyctl apps list
   flyctl apps destroy <app-name> --yes
   ```
2. Erwägen Sie ein Upgrade des Fly.io Plans

## Sicherheit

### Best Practices

1. **Secrets Management**:
   - Verwenden Sie niemals Secrets direkt im Code
   - Alle sensiblen Daten gehören in GitHub Secrets oder Fly.io Secrets

2. **Umgebungsvariablen**:
   - Secrets können über Fly.io Secrets gesetzt werden:
   ```bash
   flyctl secrets set SECRET_KEY=value --app speakstorelocate-pr-{PR_NUMBER}
   ```

3. **Zugriffskontrolle**:
   - Preview-URLs sind öffentlich zugänglich
   - Implementieren Sie ggf. Basic Auth für Preview-Umgebungen
   - Verwenden Sie keine produktiven Daten in Preview-Umgebungen

## Monitoring

### Logs anzeigen

```bash
# Live-Logs verfolgen
flyctl logs --app speakstorelocate-pr-{PR_NUMBER}

# Logs der letzten 100 Zeilen
flyctl logs --app speakstorelocate-pr-{PR_NUMBER} --lines 100
```

### Status überprüfen

```bash
# App-Status
flyctl status --app speakstorelocate-pr-{PR_NUMBER}

# VM-Status
flyctl vm status --app speakstorelocate-pr-{PR_NUMBER}
```

## Alternativen

Obwohl Fly.io die bevorzugte Lösung ist, könnten folgende Alternativen ebenfalls verwendet werden:

1. **Vercel**: 
   - Gut für Frontend-Apps
   - Eingeschränkte .NET-Unterstützung

2. **Netlify**:
   - Ähnlich wie Vercel
   - Primär für Static Sites

3. **Railway**:
   - Gute .NET-Unterstützung
   - Ähnliches Preismodell wie Fly.io

4. **Render**:
   - Unterstützt Docker
   - Automatische PR Previews verfügbar

**Hinweis**: AWS wurde explizit ausgeschlossen und wird nicht als Alternative empfohlen.

## Support und Weiterführende Links

- [Fly.io Dokumentation](https://fly.io/docs/)
- [Fly.io CLI Referenz](https://fly.io/docs/flyctl/)
- [Fly.io Preise](https://fly.io/docs/about/pricing/)
- [GitHub Actions mit Fly.io](https://fly.io/docs/app-guides/continuous-deployment-with-github-actions/)

## Changelog

- **2024-12-13**: Initiale Implementierung mit Fly.io
  - Automatisches Deployment bei PR open/sync
  - Automatisches Cleanup bei PR close
  - PR-Kommentare mit Preview-URLs
