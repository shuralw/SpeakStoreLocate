# SpeakStoreLocate

[![PR Build Validation](https://github.com/shuralw/SpeakStoreLocate/actions/workflows/pr-build-validation.yml/badge.svg)](https://github.com/shuralw/SpeakStoreLocate/actions/workflows/pr-build-validation.yml)

A .NET application for speech transcription and storage management.

## Project Structure

- **SpeakStoreLocate.ApiService** - Main API service
- **SpeakStoreLocate.AppHost** - Application host
- **SpeakStoreLocate.ServiceDefaults** - Shared service configurations
- **SpeakStoreLocate.Client** - Client application
- **SpeakStoreLocate.Tests** - Unit tests

## Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Git

## Getting Started

### Clone the Repository
```bash
git clone https://github.com/shuralw/SpeakStoreLocate.git
cd SpeakStoreLocate
```

### Restore Dependencies
```bash
dotnet restore SpeakStoreLocate.sln
```

### Build the Solution
```bash
dotnet build SpeakStoreLocate.sln --configuration Release
```

### Run Tests
```bash
dotnet test SpeakStoreLocate.sln --configuration Release
```

### Run the Application
```bash
dotnet run --project SpeakStoreLocate.ApiService/SpeakStoreLocate.ApiService.csproj
```

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details on:
- Development setup
- Building and testing
- Pull request process
- Code style guidelines

### Pull Request Requirements

All pull requests to the `master` branch must pass automated build validation:
- ✅ Code must build successfully
- ✅ All tests must pass
- ✅ No critical build errors

The PR build validation workflow runs automatically on every pull request. See [CONTRIBUTING.md](CONTRIBUTING.md) for more details.

## Branch Protection

The `master` branch is protected to ensure production stability. All changes must:
1. Go through a pull request
2. Pass automated build validation
3. Have all status checks succeed

For detailed information about branch protection settings, see [Branch Protection Configuration](.github/BRANCH_PROTECTION.md).

## Development

### Project Technologies
- .NET 10.0
- ASP.NET Core
- AWS App Runner (deployment)
- Docker

### CI/CD
- **PR Build Validation** - Runs on all pull requests to validate builds and tests
- **Deployment** - Automatically deploys to AWS App Runner on merges to master

## License

[Add your license information here]

## Support

For questions or issues:
- Open an [issue](https://github.com/shuralw/SpeakStoreLocate/issues)
- See [Contributing Guidelines](CONTRIBUTING.md)
