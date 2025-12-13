# PR Preview Environments - Setup Checklist

This checklist helps repository maintainers set up the automated PR preview environments.

## Initial Setup (One-time)

### 1. Fly.io Account Setup
- [ ] Create a Fly.io account at https://fly.io/app/sign-up
- [ ] Create or identify your organization name
- [ ] Note your organization name (e.g., "personal" or custom org name)

### 2. Generate Fly.io API Token
- [ ] Install Fly.io CLI (see [docs/PR_PREVIEW_SETUP.md](../docs/PR_PREVIEW_SETUP.md))
- [ ] Login to Fly.io: `flyctl auth login`
- [ ] Generate API token: `flyctl auth token`
- [ ] Copy the token (you'll need it in the next step)

### 3. Configure GitHub Secrets
- [ ] Go to: Repository Settings → Secrets and variables → Actions
- [ ] Click "New repository secret"
- [ ] Add `FLY_API_TOKEN` with your Fly.io API token
- [ ] Add `FLY_ORG` with your organization name

### 4. Verify Configuration Files
- [ ] Confirm `fly.toml` exists in repository root
- [ ] Confirm `.github/workflows/pr-preview.yml` exists
- [ ] Confirm Dockerfile exists at `SpeakStoreLocate.ApiService/dockerfile`

## Testing the Setup

### 5. Create a Test PR
- [ ] Create a test branch: `git checkout -b test/preview-env`
- [ ] Make a small change (e.g., update README)
- [ ] Commit and push: `git push origin test/preview-env`
- [ ] Create a Pull Request on GitHub

### 6. Verify Workflow Execution
- [ ] Navigate to: Actions tab in GitHub
- [ ] Find "PR Preview Deployment" workflow
- [ ] Wait for workflow to complete (~3-5 minutes)
- [ ] Check for any errors in the workflow logs

### 7. Verify PR Comment
- [ ] Go to your test Pull Request
- [ ] Look for a comment with "🚀 Preview Environment Deployed"
- [ ] Note the preview URL (e.g., `https://speakstorelocate-pr-1.fly.dev`)

### 8. Test Preview Environment
- [ ] Click on the preview URL
- [ ] Verify the application loads correctly
- [ ] Test basic functionality

### 9. Test Cleanup
- [ ] Close or merge the test PR
- [ ] Wait for cleanup workflow to run (~1 minute)
- [ ] Look for "🧹 Preview Environment Cleaned Up" comment
- [ ] Verify app is deleted: `flyctl apps list` (optional)

## Troubleshooting

If any step fails, consult:
- [Quick Setup Guide](../docs/PR_PREVIEW_SETUP.md)
- [Full Documentation](../docs/PR_PREVIEW_ENVIRONMENTS.md)
- GitHub Actions logs
- Fly.io logs: `flyctl logs --app speakstorelocate-pr-{NUMBER}`

## Post-Setup

### Monitor Usage
- [ ] Check Fly.io dashboard regularly for active apps
- [ ] Monitor GitHub Actions usage
- [ ] Clean up old preview apps if needed: `flyctl apps destroy <app-name> --yes`

### Team Communication
- [ ] Inform team about PR preview environments
- [ ] Share documentation links
- [ ] Provide usage examples

## Success Criteria

✅ Setup is complete when:
- GitHub Secrets are configured
- Test PR triggers automatic deployment
- Preview URL works and shows the application
- Cleanup runs when PR is closed
- Team members can access preview environments

## Support

For issues or questions:
- Check [docs/PR_PREVIEW_ENVIRONMENTS.md](../docs/PR_PREVIEW_ENVIRONMENTS.md)
- Review GitHub Actions workflow logs
- Contact Fly.io support: https://community.fly.io/
