# SpeakStoreLocate

ASP.NET Core 8.0 API Service für die SpeakStoreLocate Anwendung.

## Features

- ASP.NET Core 8.0 Web API
- Docker Containerisierung
- AWS App Runner Deployment (Produktion)
- **Automatisierte PR Preview-Umgebungen mit Azure Container Apps** 🚀

## PR Preview-Umgebungen

Jeder Pull Request erhält automatisch eine eigene isolierte Preview-Umgebung auf Azure Container Apps. Dies ermöglicht es Reviewern, Änderungen vor dem Mergen zu testen.

### Schnellstart

1. Pull Request erstellen
2. Auf automatisches Deployment warten (~5-8 Minuten)
3. Preview-URL im PR-Kommentar finden
4. Änderungen testen unter `https://speakstorelocate-pr-{NUMMER}.westeurope.azurecontainerapps.io`

### Dokumentation

- **[Schnellstart-Anleitung](docs/PR_PREVIEW_SETUP.md)** - Einrichtungsanleitung für Repository-Maintainer
- **[Vollständige Dokumentation](docs/PR_PREVIEW_ENVIRONMENTS.md)** - Komplette Dokumentation inkl. Troubleshooting
- **[Einrichtungs-Checkliste](.github/PR_PREVIEW_CHECKLIST.md)** - Schritt-für-Schritt Checkliste

### Voraussetzungen für Maintainer

Um PR Preview-Umgebungen zu aktivieren, fügen Sie diese GitHub Secrets hinzu:
- `AZURE_CREDENTIALS` - Azure Service Principal Credentials (JSON)
- `AZURE_RESOURCE_GROUP` - Azure Resource Group Name

Siehe [PR_PREVIEW_SETUP.md](docs/PR_PREVIEW_SETUP.md) für detaillierte Einrichtungsanweisungen.

## Entwicklung

### Voraussetzungen

- .NET 8.0 SDK
- Docker (optional)
- Azure CLI (für lokale Tests mit Azure)

### Projekt-Struktur

- **SpeakStoreLocate.ApiService** - Haupt-API Service
- **SpeakStoreLocate.AppHost** - .NET Aspire AppHost
- **SpeakStoreLocate.ServiceDefaults** - Gemeinsame Service-Defaults
- **SpeakStoreLocate.Client** - Client-Anwendung
- **SpeakStoreLocate.Tests** - Unit Tests

### Build

```bash
dotnet build SpeakStoreLocate.sln
```

### Lokal ausführen

```bash
cd SpeakStoreLocate.ApiService
dotnet run
```

### Docker

Docker Image bauen:

```bash
docker build -f SpeakStoreLocate.ApiService/dockerfile -t speakstorelocate .
```

Container starten:

```bash
docker run -p 8080:8080 speakstorelocate
```

## Deployment

### Produktion (AWS App Runner)

Produktions-Deployments zu AWS App Runner werden automatisch bei Push auf den `master` Branch ausgelöst.

Siehe `.github/workflows/deploy.yml` für Details.

### Preview-Umgebungen (Azure Container Apps)

Preview-Umgebungen werden automatisch für jeden Pull Request erstellt.

Siehe `.github/workflows/pr-preview.yml` für Details.

## Technologie-Stack

- **Backend**: ASP.NET Core 8.0
- **Container**: Docker
- **Produktion**: AWS App Runner
- **Preview**: Azure Container Apps
- **CI/CD**: GitHub Actions

## Lizenz

[Lizenzinformationen hier hinzufügen]
