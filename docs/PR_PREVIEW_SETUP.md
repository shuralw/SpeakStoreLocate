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
  --name speakstorelocate \
  --location germanywestcentral
```

### Schritt 1.5: Azure Resource Provider registrieren (einmalig)

Azure Container Apps benötigt registrierte Resource Provider auf Subscription-Ebene. Wenn diese noch nicht registriert sind, versucht die Azure CLI sie automatisch zu registrieren – das scheitert aber, wenn der Service Principal nur Rechte auf Resource-Group-Ebene hat.

Führen Sie diese Schritte **einmalig** mit einem Benutzer aus, der auf der Subscription mindestens **Contributor** (oder Owner) hat:

```bash
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.OperationalInsights --wait
az provider register --namespace Microsoft.ContainerRegistry --wait
```

Optional (je nach Setup):

```bash
az provider register --namespace Microsoft.ManagedIdentity --wait
```

Status prüfen:

```bash
az provider show -n Microsoft.App --query registrationState -o tsv
```

### Schritt 2: Service Principal erstellen

Erstellen Sie einen Service Principal für GitHub Actions:

```bash
# Ersetzen Sie {subscription-id} mit Ihrer Subscription ID
az ad sp create-for-rbac \
  --name "SpeakStoreLocate-PR-Preview" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/speakstorelocate \
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
| `AZURE_RESOURCE_GROUP` | `speakstorelocate` | Name der Resource Group |

Zusätzlich benötigt der Workflow eine **User Assigned Managed Identity** für den Image-Pull aus ACR (damit es nicht zu `ImagePullUnauthorized` kommt und keine ACR Passwörter nötig sind).

### Schritt 3.5: Managed Identity für ACR Pull (empfohlen, einmalig)

Diese Schritte müssen **einmalig** durchgeführt werden (mit Rechten, um Role Assignments auf dem ACR zu setzen).

```bash
# Variablen anpassen
RG="speakstorelocate"
LOCATION="germanywestcentral"
ACR_NAME="speakstorelocate"
IDENTITY_NAME="speakstorelocate-acr-pull"

# User Assigned Managed Identity erstellen
az identity create -g "$RG" -n "$IDENTITY_NAME" -l "$LOCATION"

# AcrPull auf dem ACR vergeben
ACR_ID=$(az acr show -n "$ACR_NAME" -g "$RG" --query id -o tsv)
PRINCIPAL_ID=$(az identity show -g "$RG" -n "$IDENTITY_NAME" --query principalId -o tsv)
az role assignment create --assignee "$PRINCIPAL_ID" --role AcrPull --scope "$ACR_ID"

# Resource ID (für GitHub Variable)
az identity show -g "$RG" -n "$IDENTITY_NAME" --query id -o tsv
```

### Schritt 3.6: GitHub Actions Variable setzen

Unter `Settings` → `Secrets and variables` → `Actions` → `Variables` (oder alternativ unter `Secrets`) folgende Variable hinzufügen:

> **Hinweis**: `AZURE_ACR_PULL_IDENTITY_RESOURCE_ID` kann entweder als **Variable** oder als **Secret** gespeichert werden. Der Workflow unterstützt beide Varianten. Empfohlen wird die Speicherung als Variable, da es sich um eine nicht-sensitive Resource ID handelt.

| Variable Name | Wert | Beschreibung |
|--------------|------|--------------|
| `AZURE_ACR_PULL_IDENTITY_RESOURCE_ID` | Resource ID aus Schritt 3.5 | UAMI für ACR Pull (wird als `--registry-identity` verwendet) |


Zusätzliche Secrets (für das Backend in der Preview-Umgebung):

| Secret Name | Beschreibung |
|------------|--------------|
| `AWS_S3_BUCKETNAME` | AWS S3 Bucket Name |
| `AWS_S3_ACCESSKEY` | AWS Access Key für S3 |
| `AWS_S3_SECRETKEY` | AWS Secret Key für S3 |
| `AWS_DYNAMODB_TABLENAME` | DynamoDB Table Name |
| `AWS_DYNAMODB_ACCESSKEY` | AWS Access Key für DynamoDB |
| `AWS_DYNAMODB_SECRETKEY` | AWS Secret Key für DynamoDB |
| `AWS_TRANSCRIBE_ACCESSKEY` | AWS Access Key für Transcribe |
| `AWS_TRANSCRIBE_SECRETKEY` | AWS Secret Key für Transcribe |
| `DEEPGRAM_APIKEY` | Deepgram API Key |
| `ELEVENLABS_APIKEY` | ElevenLabs API Key |
| `OPENAI_APIKEY` | OpenAI API Key |
| `OPENAI_DEFAULTMODEL` | Default Model (z.B. `gpt-4.1-nano-2025-04-14`) |

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
  az group show --name speakstorelocate
  ```

### Deployment erfolgreich, aber App startet nicht
- Überprüfen Sie die Azure Container Apps Logs:
  ```bash
  az containerapp logs show \
    --name speakstorelocate-pr-{PR_NUMMER} \
    --resource-group speakstorelocate \
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
az containerapp list --resource-group speakstorelocate --output table

# Spezifische App löschen
az containerapp delete \
  --name speakstorelocate-pr-{NUMMER} \
  --resource-group speakstorelocate \
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
     --resource-group speakstorelocate \
     --secrets "key=value"
   ```

## Azure Portal Überwachung

1. Öffnen Sie [Azure Portal](https://portal.azure.com)
2. Navigieren Sie zur Resource Group `speakstorelocate-rg`
2. Navigieren Sie zur Resource Group `speakstorelocate`
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
