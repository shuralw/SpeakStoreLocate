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

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details on:
- Development setup
- Building and testing
- Pull request process
- Code style guidelines

### Pull Request Requirements

All pull requests to the `master` branch must pass automated build validation:

**Backend PRs:**
- ✅ Backend code must build successfully
- ✅ All backend tests must pass
- ✅ Docker image validation (if Dockerfile exists)

**Frontend PRs:**
- ✅ Frontend code must build successfully
- ✅ All frontend tests must pass

The PR build validation workflow intelligently detects which parts of the codebase changed and runs the appropriate validation. See [CONTRIBUTING.md](CONTRIBUTING.md) for more details.

## Branch Protection

The `master` branch is protected to ensure production stability. All changes must:
1. Go through a pull request
2. Pass automated build validation
3. Have all status checks succeed

For detailed information about branch protection settings, see [Branch Protection Configuration](.github/BRANCH_PROTECTION.md).

## Development

### Project Technologies
- **Backend**: .NET 10.0, ASP.NET Core
- **Frontend**: Angular 15, TypeScript
- **Deployment**: AWS App Runner, Docker
- **CI/CD**: GitHub Actions

### CI/CD
- **PR Build Validation** - Separate validation for backend and frontend changes
  - Backend: .NET build and tests, Docker validation
  - Frontend: Angular build and Karma tests
- **Deployment** - Automatically deploys backend to AWS App Runner on merges to master

## License

[Add your license information here]

## Support

For questions or issues:
- Open an [issue](https://github.com/shuralw/SpeakStoreLocate/issues)
- See [Contributing Guidelines](CONTRIBUTING.md)
