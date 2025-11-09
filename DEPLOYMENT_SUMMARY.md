# Quick Deployment Summary

## Safety Notice

⚠️ **All deployments are NON-DESTRUCTIVE**
- Never deletes existing resources
- Preserves all database data
- Uses existing resource group `rg-green-squirrel`

See [DEPLOYMENT_SAFETY.md](DEPLOYMENT_SAFETY.md) for complete details.

## What Changed

### New Files Created

1. **[infrastructure/static-web-app.bicep](infrastructure/static-web-app.bicep)** - Azure Static Web App resource definition
2. **[infrastructure/hive-reader-cosmos.bicep](infrastructure/hive-reader-cosmos.bicep)** - New Cosmos DB for Hive Reader
3. **[AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md)** - Complete deployment guide
4. **[DEPLOYMENT_SAFETY.md](DEPLOYMENT_SAFETY.md)** - Comprehensive safety documentation
5. **[SETUP_NEW_AZURE_APP.ps1](SETUP_NEW_AZURE_APP.ps1)** - PowerShell script to create Azure AD App
6. **[PRE_FLIGHT_CHECK.ps1](PRE_FLIGHT_CHECK.ps1)** - Pre-flight check script
7. **[FIX_FEDERATED_CREDENTIALS.ps1](FIX_FEDERATED_CREDENTIALS.ps1)** - PowerShell script to fix OIDC credentials (requires App ID parameter)
8. **[DIAGNOSE_AZURE_APP.ps1](DIAGNOSE_AZURE_APP.ps1)** - PowerShell diagnostic script (requires App ID parameter)

### Files Modified

1. **[infrastructure/main.bicep](infrastructure/main.bicep)** - Added Static Web App and new Cosmos DB modules
2. **[.github/workflows/azure-static-web-app.yml](.github/workflows/azure-static-web-app.yml)** - Updated to use OIDC authentication

## Quick Start Deployment

### 1. Set up Azure AD App & GitHub Secrets (5 minutes)

Run the setup script:

```powershell
.\SETUP_NEW_AZURE_APP.ps1
```

This will:
- Create Azure AD App `GH-HiveReader-Deploy`
- Set up OIDC federated credentials
- Configure GitHub secrets automatically

### 2. Verify Everything is Ready (1 minute)

```powershell
.\PRE_FLIGHT_CHECK.ps1
```

This checks:
- ✅ Azure CLI installed and logged in
- ✅ Resource group exists
- ✅ GitHub secrets are configured
- ✅ All templates are present

### 3. Push to GitHub (Automatic Deployment!)

```bash
git add .
git commit -m "Configure Azure deployment"
git push origin main
```

**The GitHub Action will automatically:**
1. Deploy infrastructure (creates resources if they don't exist)
2. Build and deploy your web application

> **Note:** Infrastructure deployment is automatic and non-destructive - creates new resources, updates existing ones, never deletes.

### Alternative: Manual Infrastructure Deployment

```bash
# Create Azure AD App
az ad app create --display-name "GitHub-HiveReader-Deploy"
# Note the appId output

# Create Service Principal
az ad sp create --id 78227685-c5f3-4693-a0fa-2e8d0a711b12

# Assign Permissions
SUBSCRIPTION_ID=$(az account show --query id --output tsv)
az role assignment create \
  --assignee 78227685-c5f3-4693-a0fa-2e8d0a711b12 \
  --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-green-squirrel

# Set up Federated Credentials
az ad app federated-credential create --id 78227685-c5f3-4693-a0fa-2e8d0a711b12 --parameters '{
  "name": "GitHubMainBranch",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:crousky/hive-reader-sharp:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

az ad app federated-credential create --id 78227685-c5f3-4693-a0fa-2e8d0a711b12 --parameters '{
  "name": "GitHubPullRequests",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:crousky/hive-reader-sharp:pull_request",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

### 3. Configure GitHub Secrets (2 minutes)

Add these three secrets in GitHub repo settings:

```bash
gh secret set AZURE_CLIENT_ID --body "78227685-c5f3-4693-a0fa-2e8d0a711b12"
gh secret set AZURE_TENANT_ID --body "$(az account show --query tenantId -o tsv)"
gh secret set AZURE_SUBSCRIPTION_ID --body "$(az account show --query id -o tsv)"
```

### 4. Deploy (1 minute)

```bash
git add .
git commit -m "Configure Azure deployment"
git push origin main
```

## What Gets Deployed

- **Static Web App**: `hive-reader-web-prod` (Free tier)
- **Cosmos DB**: `hive-reader-db-prod-{suffix}` (Serverless + Free tier)
  - Database: `HiveReaderDB`
  - Containers: `Articles`, `UserPreferences`

## Resources

- **Full Guide**: [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md)
- **Bicep Templates**: [infrastructure/](infrastructure/)
- **GitHub Workflow**: [.github/workflows/azure-static-web-app.yml](.github/workflows/azure-static-web-app.yml)

## Estimated Cost

- **$0-25/month** with free tiers
  - Static Web App: Free tier
  - Cosmos DB: Free tier (1000 RU/s, 25GB)

## Support

Check the [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md) troubleshooting section if you encounter issues.
