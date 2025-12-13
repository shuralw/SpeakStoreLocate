# SpeakStoreLocate

[![PR Build Validation](https://github.com/shuralw/SpeakStoreLocate/actions/workflows/pr-build-validation.yml/badge.svg)](https://github.com/shuralw/SpeakStoreLocate/actions/workflows/pr-build-validation.yml)

A .NET application for speech transcription and storage management.

## Project Structure

- **SpeakStoreLocate.ApiService** - Main API service (Backend - .NET)
- **SpeakStoreLocate.AppHost** - Application host
- **SpeakStoreLocate.ServiceDefaults** - Shared service configurations
- **SpeakStoreLocate.Client** - Angular frontend application
- **SpeakStoreLocate.Tests** - Backend unit tests

## Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Node.js 18.x](https://nodejs.org/) or later (for frontend)
- Docker (optional, for container builds)
- Git

## Getting Started

### Backend Setup

```bash
git clone https://github.com/shuralw/SpeakStoreLocate.git
cd SpeakStoreLocate
dotnet restore SpeakStoreLocate.sln
dotnet build SpeakStoreLocate.sln --configuration Release
dotnet test SpeakStoreLocate.Tests/SpeakStoreLocate.Tests.csproj --configuration Release
```

### Frontend Setup

```bash
cd SpeakStoreLocate.Client
npm ci
npm run build -- --configuration development
npm test -- --watch=false --browsers=ChromeHeadless
```

### Run the Application

**Backend API:**

```bash
dotnet run --project SpeakStoreLocate.ApiService/SpeakStoreLocate.ApiService.csproj
```

**Frontend:**

```bash
cd SpeakStoreLocate.Client
npm start
```

## PR Preview Environments (Azure Container Apps)

Every Pull Request gets its own isolated preview environment deployed to Azure Container Apps. The workflow builds and pushes the API + Client Docker images and posts the preview URLs as a PR comment.

### Documentation

- **Setup / Checklist**: [.github/PR_PREVIEW_CHECKLIST.md](.github/PR_PREVIEW_CHECKLIST.md)
- **Full documentation**: [docs/PR_PREVIEW_ENVIRONMENTS.md](docs/PR_PREVIEW_ENVIRONMENTS.md)

### Required GitHub Secrets

- `AZURE_CREDENTIALS` (Service Principal JSON)
- `AZURE_RESOURCE_GROUP` (e.g. `speakstorelocate`)

## Docker

Build the API Docker image:

```bash
docker build -f SpeakStoreLocate.ApiService/dockerfile -t speakstorelocate-apiservice:local .
```

Build the client Docker image:

```bash
docker build -f SpeakStoreLocate.Client/dockerfile -t speakstorelocate-client:local .
```

## Deployment

### Production (AWS App Runner)

Production deployments to AWS App Runner are triggered automatically on push to the `master` branch.

See [.github/workflows/deploy.yml](.github/workflows/deploy.yml) for details.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[Add your license information here]

## Support

For questions or issues:
- Open an [issue](https://github.com/shuralw/SpeakStoreLocate/issues)
