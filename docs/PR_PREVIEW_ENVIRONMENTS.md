# PR Preview-Umgebungen - Dokumentation

Diese Dokumentation beschreibt die automatisierten Preview-Umgebungen für Feature-Branches, die mit Azure Container Apps umgesetzt wurden.

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
https://speakstorelocate-pr-{PR_NUMBER}.{region}.azurecontainerapps.io
```

Beispiele:
- PR #42: `https://speakstorelocate-pr-42.westeurope.azurecontainerapps.io`
- PR #123: `https://speakstorelocate-pr-123.westeurope.azurecontainerapps.io`

## Einrichtung

### Voraussetzungen

1. **Azure Account**: 
   - Erstellen Sie einen Account auf [Azure Portal](https://portal.azure.com)
   - Erstellen Sie ein Azure-Abonnement (falls noch nicht vorhanden)

2. **Azure CLI** (für lokale Tests):
   ```bash
   # Windows
   Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
   
   # macOS
   brew update && brew install azure-cli
   
   # Linux
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   ```

3. **Service Principal erstellen**:
   ```bash
   # Anmelden bei Azure
   az login
   
   # Service Principal erstellen
   az ad sp create-for-rbac \
     --name "SpeakStoreLocate-PR-Preview" \
     --role contributor \
     --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
     --sdk-auth
   
   # Ausgabe kopieren (wird für GitHub Secrets benötigt)
   ```

4. **Resource Group erstellen**:
   ```bash
   az group create --name speakstorelocate-rg --location westeurope
   ```

### GitHub Repository Secrets einrichten

1. Navigieren Sie zu: `Settings` → `Secrets and variables` → `Actions`
2. Klicken Sie auf `New repository secret`
3. Fügen Sie folgende Secrets hinzu:

   | Name | Wert | Beschreibung |
   |------|------|--------------|
   | `AZURE_CREDENTIALS` | JSON-Output des Service Principal | Authentifizierung für Azure |
   | `AZURE_RESOURCE_GROUP` | Name der Resource Group | z.B. "speakstorelocate-rg" |

## Konfiguration

### GitHub Workflow

Die Workflow-Datei `.github/workflows/pr-preview.yml` steuert die automatischen Deployments:

- **Trigger**: Pull Request Events (opened, synchronize, reopened, closed)
- **Deploy Job**: Erstellt/aktualisiert die Preview-Umgebung
- **Cleanup Job**: Löscht die Preview-Umgebung beim Schließen

### Azure Container Apps

Die Anwendung wird auf Azure Container Apps deployed:

```yaml
Environment: speakstorelocate-env
Region: westeurope (West Europa)
Umgebungsvariable: ASPNETCORE_ENVIRONMENT=Staging
Min Replicas: 0 (Auto-Scale auf 0)
Max Replicas: 1
Ingress: External (HTTPS)
Port: 8080
```

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
   - Nach 5-8 Minuten erscheint ein Kommentar mit der Preview-URL

### Für Reviewer

1. **Preview-URL finden**:
   - Öffnen Sie den Pull Request
   - Suchen Sie nach dem Kommentar "🚀 Preview-Umgebung bereitgestellt"
   - Klicken Sie auf die bereitgestellte URL

2. **Änderungen testen**:
   - Die Preview-Umgebung enthält die aktuellsten Änderungen aus dem PR-Branch
   - Testen Sie die neue Funktionalität
   - Geben Sie Feedback im PR

3. **Automatische Updates**:
   - Wenn der Entwickler neue Commits pusht, wird die Preview-Umgebung automatisch aktualisiert
   - Der Zeitstempel im Kommentar zeigt die letzte Aktualisierung

## Kosten und Ressourcen

### Azure Kostenmodell

- **Container Apps Free Tier**: 
  - 180.000 vCPU-Sekunden/Monat kostenlos
  - 360.000 GiB-Sekunden/Monat kostenlos
  - Ideal für Preview-Umgebungen
  
- **Scale to Zero**: 
  - Apps werden bei Inaktivität automatisch auf 0 Replicas skaliert
  - Keine Kosten bei gestoppten Apps
  - Automatischer Start bei Zugriff (Cold Start ~2-5 Sekunden)

### Ressourcen pro Preview

Jede Preview-Umgebung verwendet:
- **CPU**: 0.25 vCPU
- **RAM**: 0.5 GB
- **Region**: West Europa (westeurope)
- **Scale to Zero**: Aktiviert (spart Kosten)
- **Container Registry**: Shared ACR für alle Previews

## Troubleshooting

### Problem: Deployment schlägt fehl

**Lösung**:
1. Überprüfen Sie die GitHub Actions Logs
2. Stellen Sie sicher, dass `AZURE_CREDENTIALS` und `AZURE_RESOURCE_GROUP` korrekt gesetzt sind
3. Überprüfen Sie die Azure Portal Logs

### Problem: App startet nicht

**Lösung**:
1. Überprüfen Sie die Azure Container Apps Logs:
   ```bash
   az containerapp logs show \
     --name speakstorelocate-pr-{PR_NUMBER} \
     --resource-group {resource-group} \
     --follow
   ```
2. Prüfen Sie die Umgebungsvariablen
3. Stellen Sie sicher, dass Port 8080 korrekt exponiert wird

### Problem: Alte Preview-Umgebungen nicht gelöscht

**Lösung**:
1. Manuelle Löschung über CLI:
   ```bash
   az containerapp delete \
     --name speakstorelocate-pr-{PR_NUMBER} \
     --resource-group {resource-group} \
     --yes
   ```
2. Oder über das Azure Portal

### Problem: Image Build schlägt fehl

**Lösung**:
1. Überprüfen Sie das Dockerfile auf Fehler
2. Testen Sie den Build lokal:
   ```bash
   docker build -f SpeakStoreLocate.ApiService/dockerfile .
   ```
3. Stellen Sie sicher, dass Docker auf GitHub Actions verfügbar ist

## Sicherheit

### Best Practices

1. **Secrets Management**:
   - Verwenden Sie niemals Secrets direkt im Code
   - Alle sensiblen Daten gehören in GitHub Secrets oder Azure Key Vault

2. **Service Principal Berechtigungen**:
   - Der Service Principal sollte nur minimale Berechtigungen haben (Contributor auf Resource Group)
   - Verwenden Sie separate Service Principals für Prod und Preview

3. **Netzwerksicherheit**:
   - Preview-URLs sind öffentlich zugänglich
   - Implementieren Sie ggf. Authentication über Azure AD
   - Verwenden Sie keine produktiven Daten in Preview-Umgebungen

4. **Container Security**:
   - Images werden in Azure Container Registry gespeichert
   - ACR scannt Images automatisch auf Vulnerabilities
   - Verwenden Sie immer aktuelle Base Images

## Monitoring und Logging

### Logs anzeigen

```bash
# Live-Logs verfolgen
az containerapp logs show \
  --name speakstorelocate-pr-{PR_NUMBER} \
  --resource-group {resource-group} \
  --follow

# Logs der letzten Stunde
az containerapp logs show \
  --name speakstorelocate-pr-{PR_NUMBER} \
  --resource-group {resource-group} \
  --tail 100
```

### Status überprüfen

```bash
# App-Status
az containerapp show \
  --name speakstorelocate-pr-{PR_NUMBER} \
  --resource-group {resource-group}

# Revision Status
az containerapp revision list \
  --name speakstorelocate-pr-{PR_NUMBER} \
  --resource-group {resource-group}
```

### Azure Portal

1. Öffnen Sie [Azure Portal](https://portal.azure.com)
2. Navigieren Sie zur Resource Group
3. Wählen Sie die Container App
4. Unter "Monitoring" finden Sie:
   - Logs
   - Metriken
   - Application Insights (wenn konfiguriert)

## Vergleich zu anderen Plattformen

### Warum Azure statt Fly.io/Vercel/Netlify?

| Feature | Azure Container Apps | Fly.io | Vercel/Netlify |
|---------|---------------------|--------|----------------|
| .NET Support | ✅ Vollständig | ✅ Docker | ⚠️ Limitiert |
| Auto-Scale to Zero | ✅ | ✅ | ❌ |
| Enterprise-Ready | ✅ | ⚠️ | ⚠️ |
| Integration mit Azure | ✅ Native | ❌ | ❌ |
| Kosten Free Tier | ✅ Großzügig | ✅ Gut | ✅ Gut |
| Deutschland Region | ✅ West Europa | ✅ Frankfurt | ⚠️ Begrenzt |

### Alternative: Azure Static Web Apps

Für reine Frontend-Anwendungen könnte Azure Static Web Apps eine Alternative sein:
- Einfachere Konfiguration
- Automatische PR Previews integriert
- Nicht geeignet für Backend-APIs

**Hinweis**: AWS wurde explizit vom Anforderungsprofil ausgeschlossen.

## Support und Weiterführende Links

- [Azure Container Apps Dokumentation](https://learn.microsoft.com/en-us/azure/container-apps/)
- [Azure CLI Referenz](https://learn.microsoft.com/en-us/cli/azure/)
- [Azure Preise Calculator](https://azure.microsoft.com/en-us/pricing/calculator/)
- [GitHub Actions mit Azure](https://docs.github.com/en/actions/deployment/deploying-to-azure)

## Changelog

- **2024-12-24**: Migration von Fly.io zu Azure Container Apps
  - Automatisches Deployment via Azure CLI
  - Container Registry Integration
  - Scale to Zero für Kostenoptimierung
  - Deutsche Dokumentation

- **2024-12-13**: Initiale Implementierung mit Fly.io
  - Automatisches Deployment bei PR open/sync
  - Automatisches Cleanup bei PR close
  - PR-Kommentare mit Preview-URLs
