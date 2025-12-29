# Contributing to SpeakStoreLocate

Thank you for your interest in contributing to SpeakStoreLocate! This document provides guidelines and information to help you contribute effectively.

## Table of Contents
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Building and Testing](#building-and-testing)
- [Pull Request Process](#pull-request-process)
- [Branch Protection Policy](#branch-protection-policy)
- [Code Style](#code-style)

## Getting Started

Before contributing, please:
1. Check existing issues and pull requests to avoid duplicates
2. For major changes, open an issue first to discuss your proposal
3. Fork the repository and create a feature branch

## Development Setup

### Prerequisites
- .NET SDK 10.0 or later
- Git
- A code editor (Visual Studio, VS Code, or Rider recommended)

### Clone and Setup
```bash
git clone https://github.com/shuralw/SpeakStoreLocate.git
cd SpeakStoreLocate
dotnet restore SpeakStoreLocate.sln
```

## Building and Testing

### Building the Solution
To build the entire solution:
```bash
dotnet build SpeakStoreLocate.sln --configuration Release
```

To build a specific project:
```bash
dotnet build SpeakStoreLocate.ApiService/SpeakStoreLocate.ApiService.csproj --configuration Release
```

### Running Tests
To run all tests:
```bash
dotnet test SpeakStoreLocate.sln --configuration Release
```

To run tests for a specific project:
```bash
dotnet test SpeakStoreLocate.Tests/SpeakStoreLocate.Tests.csproj --configuration Release
```

### Running the Application Locally
```bash
dotnet run --project SpeakStoreLocate.ApiService/SpeakStoreLocate.ApiService.csproj
```

## Pull Request Process

### Before Submitting a PR
1. **Build locally**: Ensure your code builds without errors
   ```bash
   dotnet build SpeakStoreLocate.sln --configuration Release
   ```

2. **Run tests**: Verify all tests pass
   ```bash
   dotnet test SpeakStoreLocate.sln --configuration Release
   ```

3. **Commit your changes**: Use clear, descriptive commit messages
   ```bash
   git add .
   git commit -m "Add feature: description of your changes"
   ```

4. **Push to your fork**: Push your branch to your GitHub fork
   ```bash
   git push origin your-feature-branch
   ```

### Creating the Pull Request
1. Navigate to the [repository on GitHub](https://github.com/shuralw/SpeakStoreLocate)
2. Click "New Pull Request"
3. Select your fork and branch
4. Fill in the PR template with:
   - Clear description of changes
   - Related issue numbers (if applicable)
   - Testing performed
   - Any breaking changes

### PR Build Validation
**All pull requests to the `master` branch must pass automated build validation before they can be merged.**

When you submit a PR:
1. GitHub Actions will automatically trigger the "PR Build Validation" workflow
2. The workflow will:
   - Checkout your code
   - Setup the .NET environment
   - Restore dependencies
   - Build the solution
   - Run all tests
3. The PR can only be merged if all checks pass ✅

You can view the build status:
- In the PR page under "Checks"
- In the "Actions" tab of the repository

If the build fails ❌:
1. Click on the failed check to view logs
2. Fix the issues in your branch
3. Push the changes - the validation will run automatically again

## Branch Protection Policy

The `master` branch is protected with the following policies:

### Required Status Checks
- ✅ **PR Build Validation must pass** before merging
- All automated tests must succeed
- Code must build without errors

### Branch Protection Rules
- Direct pushes to `master` are discouraged (PRs are preferred)
- Pull request reviews may be required
- Status checks must pass before merging
- Force pushes are restricted

See [BRANCH_PROTECTION.md](.github/BRANCH_PROTECTION.md) for detailed configuration information.

## Code Style

### General Guidelines
- Follow standard .NET coding conventions
- Use meaningful variable and method names
- Add comments for complex logic
- Keep methods focused and concise
- Handle errors appropriately

### .NET Specific
- Use nullable reference types appropriately
- Follow async/await patterns for asynchronous code
- Use dependency injection where appropriate
- Write unit tests for new functionality

### Before Submitting
- Ensure no compiler warnings in your changes
- Fix any legitimate warnings that the build produces
- Add XML documentation for public APIs

## Questions or Issues?

If you have questions:
- Check existing documentation
- Open an issue for discussion
- Reach out to maintainers

Thank you for contributing to SpeakStoreLocate! 🎉
