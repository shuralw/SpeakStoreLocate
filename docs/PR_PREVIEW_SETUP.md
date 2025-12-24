# PR Preview-Umgebungen - Schnellstart-Anleitung

Diese Anleitung hilft Repository-Maintainern beim Einrichten der automatisierten PR Preview-Umgebungen mit Azure Container Apps.

## Voraussetzungen

1. **Azure Account**
   - Registrieren Sie sich unter [Azure Portal](https://portal.azure.com)
   - Stellen Sie sicher, dass Sie ein aktives Abonnement haben

2. **Azure CLI installieren**
   ```bash
   # Windows (PowerShell als Administrator)
   Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi
   Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
   
   # macOS
   brew update && brew install azure-cli
   
   # Linux (Ubuntu/Debian)
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   ```

3. **Bei Azure anmelden**
   ```bash
   az login
   ```

## Einrichtung in 5 Schritten

### Schritt 1: Resource Group erstellen

Erstellen Sie eine Resource Group für die Preview-Umgebungen:

```bash
az group create \
  --name speakstorelocate-rg \
  --location westeurope
```

### Schritt 2: Service Principal erstellen

Erstellen Sie einen Service Principal für GitHub Actions:

```bash
# Ersetzen Sie {subscription-id} mit Ihrer Subscription ID
az ad sp create-for-rbac \
  --name "SpeakStoreLocate-PR-Preview" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/speakstorelocate-rg \
  --sdk-auth
```

**Wichtig**: Kopieren Sie die gesamte JSON-Ausgabe! Sie wird im nächsten Schritt benötigt.

Beispiel-Ausgabe:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  ...
}
```

### Schritt 3: GitHub Secrets einrichten

1. Gehen Sie zu Ihrem GitHub Repository
2. Navigieren Sie zu: `Settings` → `Secrets and variables` → `Actions`
3. Klicken Sie auf `New repository secret`
4. Fügen Sie folgende Secrets hinzu:

| Secret Name | Wert | Beschreibung |
|------------|------|--------------|
| `AZURE_CREDENTIALS` | Die komplette JSON-Ausgabe aus Schritt 2 | Für Azure-Authentifizierung |
| `AZURE_RESOURCE_GROUP` | `speakstorelocate-rg` | Name der Resource Group |

### Schritt 4: Konfigurationsdateien prüfen

Stellen Sie sicher, dass folgende Dateien im Repository vorhanden sind:

- ✅ `.github/workflows/pr-preview.yml` - Workflow für automatisches Deployment
- ✅ `SpeakStoreLocate.ApiService/dockerfile` - Docker-Konfiguration

### Schritt 5: Test durchführen

Testen Sie die Einrichtung:

1. Erstellen Sie einen Test-Branch:
   ```bash
   git checkout -b test/preview-environment
   ```

2. Machen Sie eine kleine Änderung:
   ```bash
   echo "# Test" >> test.md
   git add test.md
   git commit -m "Test preview environment"
   git push origin test/preview-environment
   ```

3. Erstellen Sie einen Pull Request auf GitHub

4. Warten Sie, bis der Workflow abgeschlossen ist (~5-8 Minuten)

5. Überprüfen Sie den PR-Kommentar mit der Preview-URL

## Erwartetes Verhalten

### Bei PR Öffnen/Aktualisieren
- GitHub Actions Workflow wird ausgelöst
- Eine neue Azure Container App wird erstellt: `speakstorelocate-pr-{NUMMER}`
- Docker Image wird gebaut und nach Azure Container Registry gepusht
- Anwendung wird deployed
- Kommentar wird im PR gepostet/aktualisiert mit Preview-URL

### Bei PR Schließen/Mergen
- GitHub Actions Cleanup-Workflow wird ausgelöst
- Azure Container App wird gelöscht
- Bestätigungskommentar wird im PR gepostet

## Preview-URL Format

```
https://speakstorelocate-pr-{PR_NUMMER}.westeurope.azurecontainerapps.io
```

Beispiele:
- PR #1: `https://speakstorelocate-pr-1.westeurope.azurecontainerapps.io`
- PR #42: `https://speakstorelocate-pr-42.westeurope.azurecontainerapps.io`

## Troubleshooting

### Workflow schlägt fehl mit "Error: Az CLI Login failed"
- Überprüfen Sie, ob `AZURE_CREDENTIALS` Secret korrekt gesetzt ist
- Stellen Sie sicher, dass die JSON-Ausgabe vollständig ist
- Prüfen Sie, ob der Service Principal noch existiert

### Workflow schlägt fehl mit "Resource Group not found"
- Überprüfen Sie, ob `AZURE_RESOURCE_GROUP` Secret korrekt ist
- Stellen Sie sicher, dass die Resource Group existiert:
  ```bash
  az group show --name speakstorelocate-rg
  ```

### Deployment erfolgreich, aber App startet nicht
- Überprüfen Sie die Azure Container Apps Logs:
  ```bash
  az containerapp logs show \
    --name speakstorelocate-pr-{PR_NUMMER} \
    --resource-group speakstorelocate-rg \
    --follow
  ```
- Prüfen Sie, ob das Dockerfile lokal funktioniert:
  ```bash
  docker build -f SpeakStoreLocate.ApiService/dockerfile .
  docker run -p 8080:8080 <image-name>
  ```

### Manuelle Bereinigung (falls nötig)
```bash
# Alle Container Apps auflisten
az containerapp list --resource-group speakstorelocate-rg --output table

# Spezifische App löschen
az containerapp delete \
  --name speakstorelocate-pr-{NUMMER} \
  --resource-group speakstorelocate-rg \
  --yes
```

## Kostenüberlegungen

### Azure Container Apps Free Tier
- 180.000 vCPU-Sekunden pro Monat (kostenlos)
- 360.000 GiB-Sekunden pro Monat (kostenlos)
- Ausgehender Datenverkehr: Erste 5 GB kostenlos

### Preview-Umgebung Ressourcen
- **CPU**: 0.25 vCPU
- **RAM**: 0.5 GB
- **Storage**: Ephemeral (keine zusätzlichen Kosten)
- **Scale to Zero**: Aktiviert

### Kosten-Management Tipps
1. Schließen Sie PRs, wenn fertig (automatische Bereinigung)
2. `Scale to Zero` ist aktiviert (keine Kosten im Leerlauf)
3. Überwachen Sie aktive Apps: `az containerapp list`
4. Löschen Sie alte Previews manuell bei Bedarf

## Sicherheitshinweise

⚠️ **Wichtige Sicherheitsüberlegungen**:

1. **Öffentlicher Zugriff**: Preview-URLs sind öffentlich zugänglich
2. **Keine Produktionsdaten**: Verwenden Sie niemals Produktionsdaten in Previews
3. **Secrets-Management**: Nutzen Sie GitHub Secrets oder Azure Key Vault
4. **Service Principal**: Minimal erforderliche Berechtigungen vergeben
5. **Umgebungsvariablen**: Über Azure Container Apps Secrets setzen:
   ```bash
   az containerapp secret set \
     --name speakstorelocate-pr-{NUMMER} \
     --resource-group speakstorelocate-rg \
     --secrets "key=value"
   ```

## Azure Portal Überwachung

1. Öffnen Sie [Azure Portal](https://portal.azure.com)
2. Navigieren Sie zur Resource Group `speakstorelocate-rg`
3. Hier sehen Sie:
   - Alle aktiven Container Apps (Preview-Umgebungen)
   - Container App Environment
   - Azure Container Registry
   - Logs und Metriken

## Zusätzliche Ressourcen

- [Vollständige Dokumentation](./PR_PREVIEW_ENVIRONMENTS.md)
- [Azure Container Apps Docs](https://learn.microsoft.com/en-us/azure/container-apps/)
- [GitHub Actions mit Azure](https://docs.github.com/en/actions/deployment/deploying-to-azure)
- [Azure CLI Referenz](https://learn.microsoft.com/en-us/cli/azure/)

## Erfolg!

✅ Die Einrichtung ist abgeschlossen, wenn:
- GitHub Secrets konfiguriert sind
- Test-PR automatisches Deployment auslöst
- Preview-URL funktioniert und zeigt die Anwendung
- Bereinigung läuft, wenn PR geschlossen wird
- Team-Mitglieder können auf Preview-Umgebungen zugreifen
