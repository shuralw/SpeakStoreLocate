# PR Preview Environments - Quick Setup Guide

## Prerequisites

1. **Fly.io Account**
   - Sign up at [fly.io](https://fly.io/app/sign-up)
   - Create or use an existing organization

2. **Install Fly.io CLI** (for testing)
   ```bash
   # macOS/Linux
   curl -L https://fly.io/install.sh | sh

   # Windows (PowerShell)
   iwr https://fly.io/install.ps1 -useb | iex
   ```

3. **Generate Fly.io API Token**
   ```bash
   flyctl auth login
   flyctl auth token
   ```

## GitHub Repository Setup

### Step 1: Add Repository Secrets

Navigate to: `Settings` → `Secrets and variables` → `Actions` → `New repository secret`

Add the following secrets:

| Secret Name | Description | How to Get |
|------------|-------------|------------|
| `FLY_API_TOKEN` | Fly.io API token for authentication | Run `flyctl auth token` |
| `FLY_ORG` | Your Fly.io organization name | Found in Fly.io dashboard or use `personal` |

### Step 2: Verify Configuration Files

Ensure these files exist in your repository:

1. **fly.toml** - Fly.io configuration
2. **.github/workflows/pr-preview.yml** - GitHub Actions workflow
3. **SpeakStoreLocate.ApiService/dockerfile** - Docker configuration

### Step 3: Test the Workflow

1. Create a feature branch:
   ```bash
   git checkout -b test/preview-environment
   ```

2. Make a small change and push:
   ```bash
   echo "# Test" >> test.md
   git add test.md
   git commit -m "Test preview environment"
   git push origin test/preview-environment
   ```

3. Create a Pull Request on GitHub

4. Wait for the workflow to complete (~3-5 minutes)

5. Look for a comment on the PR with the preview URL

## Expected Behavior

### When PR is Opened/Updated
- GitHub Actions workflow triggers
- A new Fly.io app is created: `speakstorelocate-pr-{NUMBER}`
- Application is deployed
- Comment is posted/updated on PR with preview URL

### When PR is Closed/Merged
- GitHub Actions cleanup workflow triggers
- Fly.io app is destroyed
- Confirmation comment is posted on PR

## Preview URL Format

```
https://speakstorelocate-pr-{PR_NUMBER}.fly.dev
```

Examples:
- PR #1: `https://speakstorelocate-pr-1.fly.dev`
- PR #42: `https://speakstorelocate-pr-42.fly.dev`

## Troubleshooting

### Workflow Fails with "Error: No token provided"
- Ensure `FLY_API_TOKEN` secret is set correctly
- Token must have appropriate permissions

### Workflow Fails with "Error: Organization not found"
- Verify `FLY_ORG` secret is set correctly
- Check organization name in Fly.io dashboard

### Deployment Succeeds but App Doesn't Start
- Check Fly.io logs:
  ```bash
  flyctl logs --app speakstorelocate-pr-{PR_NUMBER}
  ```
- Verify Dockerfile builds correctly locally:
  ```bash
  docker build -f SpeakStoreLocate.ApiService/dockerfile .
  ```

### Manual Cleanup (if needed)
```bash
# List all apps
flyctl apps list

# Delete specific app
flyctl apps destroy speakstorelocate-pr-{NUMBER} --yes
```

## Cost Considerations

### Fly.io Free Tier
- 3 shared-cpu-1x VMs with 256MB RAM (free)
- Up to 160GB outbound data transfer per month
- Auto-stop when idle (no charges when stopped)

### Preview Environment Resources
- **CPU**: 1 shared core
- **RAM**: 1GB
- **Storage**: Minimal (ephemeral)
- **Auto-stop**: Enabled

### Cost Management Tips
1. Close PRs when done (auto-cleanup runs)
2. Set `auto_stop_machines = true` (already configured)
3. Monitor active apps: `flyctl apps list`
4. Manually cleanup old previews if needed

## Security Notes

⚠️ **Important Security Considerations**:

1. **Public Access**: Preview URLs are publicly accessible
2. **No Production Data**: Never use production data in previews
3. **Secrets Management**: Use GitHub Secrets or Fly.io Secrets
4. **Environment Variables**: Set via Fly.io secrets:
   ```bash
   flyctl secrets set KEY=value --app speakstorelocate-pr-{NUMBER}
   ```

## Additional Resources

- [Full Documentation (German)](./PR_PREVIEW_ENVIRONMENTS.md)
- [Fly.io Documentation](https://fly.io/docs/)
- [GitHub Actions with Fly.io](https://fly.io/docs/app-guides/continuous-deployment-with-github-actions/)
