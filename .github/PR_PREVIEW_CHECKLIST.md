# PR Preview-Umgebungen - Einrichtungs-Checkliste

Diese Checkliste hilft Repository-Maintainern bei der Einrichtung der automatisierten PR Preview-Umgebungen.

## Erstmalige Einrichtung

### 1. Azure Account Einrichtung
- [ ] Azure Account erstellen unter https://portal.azure.com
- [ ] Aktives Azure-Abonnement haben oder erstellen
- [ ] Abonnement-ID notieren

### 2. Azure CLI installieren und anmelden
- [ ] Azure CLI installieren (siehe [docs/PR_PREVIEW_SETUP.md](../docs/PR_PREVIEW_SETUP.md))
- [ ] Bei Azure anmelden: `az login`
- [ ] Richtiges Abonnement auswählen: `az account set --subscription {subscription-id}`

### 2.5 Azure Resource Provider registrieren (einmalig)

- [ ] Provider registrieren (benötigt Subscription-Rechte; z.B. Contributor/Owner):
  ```bash
  az provider register --namespace Microsoft.App --wait
  az provider register --namespace Microsoft.OperationalInsights --wait
  az provider register --namespace Microsoft.ContainerRegistry --wait
  ```
- [ ] Optional (je nach Setup):
  ```bash
  az provider register --namespace Microsoft.ManagedIdentity --wait
  ```

### 3. Resource Group erstellen
- [ ] Resource Group erstellen:
  ```bash
  az group create --name speakstorelocate --location germanywestcentral
  ```
- [ ] Erstellung bestätigen:
  ```bash
  az group show --name speakstorelocate
  ```

### 4. Service Principal erstellen
- [ ] Service Principal erstellen:
  ```bash
  az ad sp create-for-rbac \
    --name "SpeakStoreLocate-PR-Preview" \
    --role contributor \
    --scopes /subscriptions/{subscription-id}/resourceGroups/speakstorelocate \
    --sdk-auth
  ```
- [ ] Komplette JSON-Ausgabe kopieren (wird für GitHub Secrets benötigt)
- [ ] Service Principal ID notieren für spätere Referenz

### 5. GitHub Secrets konfigurieren
- [ ] Zu Repository navigieren: Settings → Secrets and variables → Actions
- [ ] "New repository secret" klicken
- [ ] `AZURE_CREDENTIALS` hinzufügen mit kompletter JSON-Ausgabe
- [ ] `AZURE_RESOURCE_GROUP` hinzufügen mit Wert `speakstorelocate`

- [ ] GitHub Actions Variable hinzufügen (empfohlen, für ACR Pull ohne Passwort):
  - [ ] `AZURE_ACR_PULL_IDENTITY_RESOURCE_ID` (Resource ID einer User Assigned Managed Identity mit `AcrPull` auf dem ACR)

- [ ] Backend-Secrets hinzufügen (für API-Integrationen in Preview):
  - [ ] `AWS_S3_BUCKETNAME`
  - [ ] `AWS_S3_ACCESSKEY`
  - [ ] `AWS_S3_SECRETKEY`
  - [ ] `AWS_DYNAMODB_TABLENAME`
  - [ ] `AWS_DYNAMODB_ACCESSKEY`
  - [ ] `AWS_DYNAMODB_SECRETKEY`
  - [ ] `AWS_TRANSCRIBE_ACCESSKEY`
  - [ ] `AWS_TRANSCRIBE_SECRETKEY`
  - [ ] `DEEPGRAM_APIKEY`
  - [ ] `ELEVENLABS_APIKEY`
  - [ ] `OPENAI_APIKEY`
  - [ ] `OPENAI_DEFAULTMODEL`

### 6. Konfigurationsdateien prüfen
- [ ] Bestätigen, dass `.github/workflows/pr-preview.yml` existiert
- [ ] Bestätigen, dass Dockerfile existiert unter `SpeakStoreLocate.ApiService/dockerfile`
- [ ] Workflow-Datei auf korrekte Secrets-Namen prüfen

## Einrichtung testen

### 7. Test-PR erstellen
- [ ] Test-Branch erstellen: `git checkout -b test/preview-env`
- [ ] Kleine Änderung machen (z.B. README aktualisieren)
- [ ] Committen und pushen:
  ```bash
  git add .
  git commit -m "Test preview environment setup"
  git push origin test/preview-env
  ```
- [ ] Pull Request auf GitHub erstellen

### 8. Workflow-Ausführung überprüfen
- [ ] Zum Actions-Tab in GitHub navigieren
- [ ] "PR Preview Deployment (Azure)" Workflow finden
- [ ] Warten, bis Workflow abgeschlossen ist (~5-8 Minuten)
- [ ] Auf Fehler in den Workflow-Logs prüfen

### 9. PR-Kommentar überprüfen
- [ ] Zum Test-Pull-Request gehen
- [ ] Nach Kommentar "🚀 Preview-Umgebung bereitgestellt" suchen
- [ ] Preview-URL notieren (z.B. `https://speakstorelocate-api-pr-1.germanywestcentral.azurecontainerapps.io`)

### 10. Preview-Umgebung testen
- [ ] Auf Preview-URL klicken
- [ ] Prüfen, ob Anwendung korrekt lädt
- [ ] Basis-Funktionalität testen
- [ ] Überprüfen, dass die richtige Umgebung läuft (Staging)

### 11. Bereinigung testen
- [ ] Test-PR schließen oder mergen
- [ ] Warten, bis Cleanup-Workflow läuft (~1 Minute)
- [ ] Nach "🧹 Preview-Umgebung bereinigt" Kommentar suchen
- [ ] Optional: App-Löschung verifizieren:
  ```bash
  az containerapp list --resource-group speakstorelocate-rg --output table
  ```

## Troubleshooting

Falls ein Schritt fehlschlägt, konsultieren Sie:
- [Schnellstart-Anleitung](../docs/PR_PREVIEW_SETUP.md)
- [Vollständige Dokumentation](../docs/PR_PREVIEW_ENVIRONMENTS.md)
- GitHub Actions Logs
- Azure Container Apps Logs:
  ```bash
  az containerapp logs show \
    --name speakstorelocate-pr-{NUMMER} \
    --resource-group speakstorelocate \
    --follow
  ```

### Häufige Probleme

**Problem**: Service Principal-Erstellung schlägt fehl
- **Lösung**: Stellen Sie sicher, dass Sie ausreichende Berechtigungen haben (Owner oder User Access Administrator)

**Problem**: Resource Group nicht gefunden
- **Lösung**: Prüfen Sie den Namen und stellen Sie sicher, dass Sie im richtigen Abonnement sind

**Problem**: Workflow kann sich nicht bei Azure anmelden
- **Lösung**: Überprüfen Sie `AZURE_CREDENTIALS` Secret auf Vollständigkeit und korrekte Formatierung

**Problem**: Docker Build schlägt fehl
- **Lösung**: Testen Sie das Dockerfile lokal und prüfen Sie auf Syntax-Fehler

## Nach der Einrichtung

### Nutzung überwachen
- [ ] Azure Portal regelmäßig auf aktive Apps prüfen
- [ ] GitHub Actions Nutzung überwachen
- [ ] Alte Preview-Apps bei Bedarf manuell löschen:
  ```bash
  az containerapp delete \
    --name <app-name> \
    --resource-group speakstorelocate-rg \
    --yes
  ```

### Team-Kommunikation
- [ ] Team über PR Preview-Umgebungen informieren
- [ ] Dokumentations-Links teilen
- [ ] Verwendungsbeispiele bereitstellen
- [ ] Best Practices kommunizieren

### Kosten überwachen
- [ ] Azure Cost Management + Billing im Portal öffnen
- [ ] Budget-Alerts einrichten (empfohlen)
- [ ] Monatliche Kosten für Resource Group überwachen
- [ ] Ungenutzte Ressourcen regelmäßig bereinigen

## Erfolgskriterien

✅ Die Einrichtung ist abgeschlossen, wenn:
- GitHub Secrets korrekt konfiguriert sind
- Test-PR automatisches Deployment auslöst
- Preview-URL funktioniert und zeigt die Anwendung
- Bereinigung läuft, wenn PR geschlossen wird
- Team-Mitglieder können auf Preview-Umgebungen zugreifen
- Keine Fehler in den Workflow-Logs auftreten

## Support

Bei Problemen oder Fragen:
- Dokumentation prüfen: [docs/PR_PREVIEW_ENVIRONMENTS.md](../docs/PR_PREVIEW_ENVIRONMENTS.md)
- GitHub Actions Workflow-Logs überprüfen
- Azure Portal Logs und Metriken einsehen
- Azure Support kontaktieren: https://azure.microsoft.com/support/

## Weiterführende Schritte

Nach erfolgreicher Einrichtung:
- [ ] Produktions-Deployment separat konfigurieren
- [ ] Azure Application Insights für Monitoring einrichten
- [ ] Azure Key Vault für Secrets-Management nutzen
- [ ] Custom Domain für Preview-Umgebungen konfigurieren (optional)
- [ ] Azure AD Authentication hinzufügen (optional)
