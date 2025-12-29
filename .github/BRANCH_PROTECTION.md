# Branch Protection Configuration

This document describes the branch protection policy for the `master` branch and how to configure it in GitHub.

## Overview

The `master` branch is the main production branch of this repository. To ensure stability and prevent disruptions, all changes must go through a pull request process with automated build validation.

## Protection Rules for Master Branch

### Required Status Checks

All pull requests targeting the `master` branch must pass the following status checks before merging:

- **PR Build Validation** - Automated workflow that:
  - Restores .NET dependencies
  - Builds the solution in Release configuration
  - Runs all unit tests
  - Fails if any step encounters errors

### Benefits

- ✅ **Prevents broken builds** on the master branch
- ✅ **Ensures all tests pass** before merging
- ✅ **Maintains production quality** code
- ✅ **Catches issues early** in the development process
- ✅ **Provides clear feedback** to contributors

## Configuring Branch Protection (For Repository Administrators)

### Step 1: Access Branch Protection Settings

1. Go to the repository on GitHub: `https://github.com/shuralw/SpeakStoreLocate`
2. Click on **Settings** (requires admin access)
3. Navigate to **Branches** in the left sidebar
4. Find **Branch protection rules** section

### Step 2: Add/Edit Protection Rule for Master

1. Click **Add rule** (or **Edit** if a rule already exists for `master`)
2. In **Branch name pattern**, enter: `master`

### Step 3: Configure Required Settings

Enable the following options:

#### ✅ Require a pull request before merging
- This ensures all changes go through code review process
- Optional: Set required approvals (e.g., 1 reviewer)

#### ✅ Require status checks to pass before merging
- Check: **Require branches to be up to date before merging**
- In the search box under **Status checks that are required**, add:
  - `backend-build-and-test` (for backend validation)
  - `frontend-build-and-test` (for frontend validation)
  
  **Note**: The status checks will only appear in the list after the workflow has run at least once. You may need to:
  1. Create a test PR first to trigger the workflow
  2. Then add the status checks to branch protection
  3. Both checks are required to ensure comprehensive validation

#### ✅ Do not allow bypassing the above settings
- This ensures even administrators follow the same rules

### Step 4: Additional Recommended Settings

#### Optional but Recommended:
- ✅ **Require conversation resolution before merging** - Ensures all PR comments are addressed
- ✅ **Require linear history** - Keeps a clean commit history
- ✅ **Include administrators** - Makes rules apply to everyone (recommended for consistency)

#### Usually Not Needed:
- Require signed commits (optional, adds security)
- Require deployments to succeed (not applicable here)

### Step 5: Save Changes

1. Scroll to the bottom
2. Click **Create** (or **Save changes**)

## Verification

After configuring branch protection:

1. **Create a test PR** to the master branch
2. Verify that:
   - The "PR Build Validation" workflow runs automatically
   - The PR shows required checks in the "Checks" section
   - The "Merge" button is disabled until checks pass
   - After checks pass, the "Merge" button becomes enabled

## Workflow Details

### PR Build Validation Workflow

**Location**: `.github/workflows/pr-build-validation.yml`

**Trigger**: Pull requests targeting the `master` branch

**Jobs**:

#### Backend Build and Test
Runs when backend files are modified:
- `SpeakStoreLocate.ApiService/**`
- `SpeakStoreLocate.AppHost/**`
- `SpeakStoreLocate.ServiceDefaults/**`
- `SpeakStoreLocate.Tests/**`
- `SpeakStoreLocate.sln`
- `global.json`

**Steps**:
1. **Checkout Code** - Clones the PR branch
2. **Check for backend changes** - Uses path filters to detect backend changes
3. **Setup .NET** - Installs .NET SDK 10.0.x
4. **Restore Dependencies** - Runs `dotnet restore`
5. **Build Solution** - Builds in Release configuration
6. **Run Tests** - Executes all unit tests
7. **Build Docker Image** - Validates Docker build (if Dockerfile exists)

#### Frontend Build and Test
Runs when frontend files are modified:
- `SpeakStoreLocate.Client/**`

**Steps**:
1. **Checkout Code** - Clones the PR branch
2. **Check for frontend changes** - Uses path filters to detect frontend changes
3. **Setup Node.js** - Installs Node.js 18.x
4. **Install Dependencies** - Runs `npm ci`
5. **Build Frontend** - Builds Angular application
6. **Run Tests** - Executes frontend unit tests with Karma

**Duration**: 
- Backend: Typically 1-3 minutes
- Frontend: Typically 2-4 minutes

**Failure Scenarios**:
- Compilation errors in the code
- Failing unit tests
- Missing dependencies
- Invalid project configuration
- Docker build failures (backend only, if Dockerfile present)

## Troubleshooting

### Status Check Not Appearing

If the `backend-build-and-test` or `frontend-build-and-test` status checks don't appear in the branch protection settings:

1. Create a test PR to master that modifies both backend and frontend files
2. Wait for both workflow jobs to complete
3. Return to branch protection settings
4. Both status checks should now be available in the search

### Workflow Not Running

If the workflow doesn't trigger on PRs:

1. Check `.github/workflows/pr-build-validation.yml` exists
2. Verify the workflow syntax is valid
3. Ensure the PR targets the `master` branch
4. Check if your changes match the path filters (backend or frontend paths)
5. Check the Actions tab for any workflow errors

### Build Failing on PR

If your PR build is failing:

1. Click on the "Details" link next to the failed check
2. Review the workflow logs to identify the error
3. Fix the issue locally:
   
   **For backend:**
   ```bash
   dotnet build SpeakStoreLocate.sln --configuration Release
   dotnet test SpeakStoreLocate.Tests/SpeakStoreLocate.Tests.csproj --configuration Release
   ```
   
   **For frontend:**
   ```bash
   cd SpeakStoreLocate.Client
   npm run build -- --configuration development
   npm test -- --watch=false --browsers=ChromeHeadless
   ```
4. Commit and push the fix
5. The workflow will automatically re-run

## Bypassing Protection (Emergency Only)

In rare emergency situations, repository administrators can:

1. Temporarily disable branch protection
2. Make critical hotfixes
3. Re-enable protection immediately after

**⚠️ This should only be done in critical production incidents.**

## Maintenance

### Regular Reviews

- Periodically review branch protection settings
- Update as repository needs evolve
- Ensure workflows are functioning correctly

### Updating Workflows

When updating `.github/workflows/pr-build-validation.yml`:

1. Test changes in a fork or feature branch first
2. Ensure the job names (`backend-build-and-test` and `frontend-build-and-test`) remain consistent
3. If changing the job name, update branch protection settings accordingly

## References

- [GitHub Branch Protection Documentation](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Contributing Guidelines](../CONTRIBUTING.md)

## Summary

The branch protection policy ensures that:
- All PRs are validated before merge
- Master branch remains stable
- Contributors receive immediate feedback
- Production quality is maintained

This is a **critical security and stability feature** - do not disable without careful consideration.
