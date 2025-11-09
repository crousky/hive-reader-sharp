# Quick Deployment Summary

## What Changed

### New Files Created

1. **[infrastructure/static-web-app.bicep](infrastructure/static-web-app.bicep)** - Azure Static Web App resource definition
2. **[infrastructure/hive-reader-cosmos.bicep](infrastructure/hive-reader-cosmos.bicep)** - New Cosmos DB for Hive Reader
3. **[AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md)** - Complete deployment guide

### Files Modified

1. **[infrastructure/main.bicep](infrastructure/main.bicep)** - Added Static Web App and new Cosmos DB modules
2. **[.github/workflows/azure-static-web-app.yml](.github/workflows/azure-static-web-app.yml)** - Updated to use OIDC authentication

## Quick Start Deployment

### 1. Deploy Infrastructure (5 minutes)

```bash
az login
az group create --name rg-green-squirrel --location eastus2
az deployment group create \
  --resource-group rg-green-squirrel \
  --template-file infrastructure/main.bicep \
  --parameters environmentName=prod
```

### 2. Set up GitHub OIDC (10 minutes)

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

# Set up Federated Credentials (replace GITHUB_ORG and GITHUB_REPO)
GITHUB_ORG="your-org"
GITHUB_REPO="hive-reader-sharp"

az ad app federated-credential create --id 78227685-c5f3-4693-a0fa-2e8d0a711b12 --parameters '{
  "name": "GitHubMainBranch",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:'"$GITHUB_ORG"'/'"$GITHUB_REPO"':ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

az ad app federated-credential create --id 78227685-c5f3-4693-a0fa-2e8d0a711b12 --parameters '{
  "name": "GitHubPullRequests",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:'"$GITHUB_ORG"'/'"$GITHUB_REPO"':pull_request",
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
