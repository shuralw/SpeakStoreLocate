# SpeakStoreLocate

[![PR Build Validation](https://github.com/shuralw/SpeakStoreLocate/actions/workflows/pr-build-validation.yml/badge.svg)](https://github.com/shuralw/SpeakStoreLocate/actions/workflows/pr-build-validation.yml)

SpeakStoreLocate ist eine .NET-Anwendung für Speech-Transkription und Storage-Management.

## Features

- ASP.NET Core Web API (.NET 10)
- Angular Frontend
- Docker Containerisierung
- Produktion: AWS App Runner
- PR Preview-Umgebungen: Azure Container Apps (API + Client)

## Projektstruktur

- **SpeakStoreLocate.ApiService** - API Service (Backend)
- **SpeakStoreLocate.AppHost** - Application Host
- **SpeakStoreLocate.ServiceDefaults** - Shared service configurations
- **SpeakStoreLocate.Client** - Angular Frontend
- **SpeakStoreLocate.Tests** - Unit Tests

## Voraussetzungen

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) oder neuer
- [Node.js 18.x](https://nodejs.org/) oder neuer
- Docker (optional, für Container Builds)
- Azure CLI (optional, für lokale Tests)

## Lokale Entwicklung

### Backend

```bash
dotnet restore SpeakStoreLocate.sln
dotnet build SpeakStoreLocate.sln --configuration Release
dotnet test SpeakStoreLocate.Tests/SpeakStoreLocate.Tests.csproj --configuration Release

dotnet run --project SpeakStoreLocate.ApiService/SpeakStoreLocate.ApiService.csproj
```

### Frontend

```bash
cd SpeakStoreLocate.Client
npm ci
npm start
```

## Docker

API Image bauen (Build-Context ist Repo-Root):

```bash
docker build -f SpeakStoreLocate.ApiService/dockerfile -t speakstorelocate-apiservice:local .
```

Client Image bauen (Build-Context ist Repo-Root):

```bash
docker build -f SpeakStoreLocate.Client/dockerfile -t speakstorelocate-client:local .
```

## PR Preview-Umgebungen (Azure Container Apps)

Jeder Pull Request erhält automatisch eine isolierte Preview-Umgebung in Azure Container Apps. Der Workflow deployed API + Client und postet die URLs als PR-Kommentar.

### Dokumentation

- [docs/PR_PREVIEW_ENVIRONMENTS.md](docs/PR_PREVIEW_ENVIRONMENTS.md)
- [docs/PR_PREVIEW_SETUP.md](docs/PR_PREVIEW_SETUP.md)
- [.github/PR_PREVIEW_CHECKLIST.md](.github/PR_PREVIEW_CHECKLIST.md)

### Erforderliche GitHub Secrets

- `AZURE_CREDENTIALS` (Service Principal JSON)
- `AZURE_RESOURCE_GROUP` (z.B. `speakstorelocate`)

## Deployment

### Produktion (AWS App Runner)

Produktions-Deployments zu AWS App Runner werden automatisch bei Push auf den `master` Branch ausgelöst.

Siehe [.github/workflows/deploy.yml](.github/workflows/deploy.yml).

### Preview (Azure Container Apps)

Siehe [.github/workflows/pr-preview.yml](.github/workflows/pr-preview.yml).

## Contributing

Siehe [CONTRIBUTING.md](CONTRIBUTING.md).

## Support

Für Fragen oder Probleme:
- Issue erstellen: https://github.com/shuralw/SpeakStoreLocate/issues

## Lizenz

[Lizenzinformationen hier hinzufügen]
