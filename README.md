# SpeakStoreLocate

ASP.NET Core 8.0 API service for the SpeakStoreLocate application.

## Features

- ASP.NET Core 8.0 Web API
- Docker containerization
- AWS App Runner deployment (production)
- **Automated PR Preview Environments with Fly.io** 🚀

## PR Preview Environments

Every Pull Request automatically gets its own isolated preview environment deployed to Fly.io. This allows reviewers to test changes before merging.

### Quick Start

1. Create a Pull Request
2. Wait for the automated deployment (~3-5 minutes)
3. Find the preview URL in the PR comment
4. Test your changes at `https://speakstorelocate-pr-{NUMBER}.fly.dev`

### Documentation

- **[Quick Setup Guide](docs/PR_PREVIEW_SETUP.md)** - Setup instructions for repository maintainers
- **[Full Documentation (German)](docs/PR_PREVIEW_ENVIRONMENTS.md)** - Complete documentation including troubleshooting

### Requirements for Maintainers

To enable PR preview environments, add these GitHub Secrets:
- `FLY_API_TOKEN` - Fly.io API token
- `FLY_ORG` - Fly.io organization name

See [PR_PREVIEW_SETUP.md](docs/PR_PREVIEW_SETUP.md) for detailed setup instructions.

## Development

### Prerequisites

- .NET 8.0 SDK
- Docker (optional)

### Project Structure

- **SpeakStoreLocate.ApiService** - Main API service
- **SpeakStoreLocate.AppHost** - .NET Aspire AppHost
- **SpeakStoreLocate.ServiceDefaults** - Shared service defaults
- **SpeakStoreLocate.Client** - Client application
- **SpeakStoreLocate.Tests** - Unit tests

### Building

```bash
dotnet build SpeakStoreLocate.sln
```

### Running Locally

```bash
cd SpeakStoreLocate.ApiService
dotnet run
```

### Docker

Build the Docker image:

```bash
docker build -f SpeakStoreLocate.ApiService/dockerfile -t speakstorelocate .
```

Run the container:

```bash
docker run -p 8080:8080 speakstorelocate
```

## Deployment

### Production (AWS App Runner)

Production deployments to AWS App Runner are triggered automatically on push to the `master` branch.

See `.github/workflows/deploy.yml` for details.

### Preview Environments (Fly.io)

Preview environments are automatically created for each Pull Request.

See `.github/workflows/pr-preview.yml` for details.

## License

[Add your license information here]
