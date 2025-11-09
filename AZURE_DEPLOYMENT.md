# Azure Deployment Guide for Hive Reader

This guide walks you through deploying the Hive Reader application to Azure using Infrastructure as Code (Bicep) and GitHub Actions with OIDC authentication.

## Prerequisites

- Azure CLI installed ([Installation Guide](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli))
- Azure subscription with appropriate permissions
- GitHub repository with admin access

## Architecture

The deployment includes:

- **Azure Static Web App** (`hive-reader-web-prod`) - Hosts the Astro frontend
- **Cosmos DB** (`hive-reader-db-prod-{suffix}`) - NoSQL database for storing articles and user preferences
- **GitHub Actions** - CI/CD pipeline with OIDC authentication (no manual secrets!)

## Step 1: Login to Azure

```bash
az login
az account set --subscription "<YOUR_SUBSCRIPTION_ID>"
```

## Step 2: Create Resource Group (if not exists)

```bash
az group create \
  --name rg-green-squirrel \
  --location eastus2
```

> **Note:** All resources are deployed to `eastus2` region.

## Step 3: Deploy Infrastructure

Deploy the Bicep template to create all Azure resources:

```bash
az deployment group create \
  --resource-group rg-green-squirrel \
  --template-file infrastructure/main.bicep \
  --parameters environmentName=prod
```

> **Note:** The location parameter is optional and defaults to the resource group location (`eastus2`).

This will create:

- Static Web App: `hive-reader-web-prod`
- Cosmos DB Account: `hive-reader-db-prod-{uniqueString}`
- Cosmos DB Database: `HiveReaderDB`
- Containers: `Articles`, `UserPreferences`

### Save the deployment outputs

After deployment completes, save these important outputs:

```bash
# Get the Static Web App URL
az deployment group show \
  --resource-group rg-green-squirrel \
  --name main \
  --query properties.outputs.staticWebAppUrl.value \
  --output tsv

# Get Cosmos DB connection details
az deployment group show \
  --resource-group rg-green-squirrel \
  --name main \
  --query properties.outputs.hiveReaderCosmosEndpoint.value \
  --output tsv
```

## Step 4: Set up GitHub OIDC Federation

GitHub Actions uses OIDC to authenticate with Azure without storing secrets. Follow these steps:

### 4.1: Get your Azure Subscription and Tenant IDs

```bash
# Get Subscription ID
az account show --query id --output tsv

# Get Tenant ID
az account show --query tenantId --output tsv
```

### 4.2: Create an Azure AD Application

```bash
# Create the app registration
az ad app create --display-name "GitHub-HiveReader-Deploy"
```

Save the `appId` from the output - this is your **Client ID**.

### 4.3: Create a Service Principal

```bash
# Replace 78227685-c5f3-4693-a0fa-2e8d0a711b12 with the appId from previous step
az ad sp create --id 78227685-c5f3-4693-a0fa-2e8d0a711b12
```

### 4.4: Assign Permissions to the Service Principal

Grant the service principal access to your resource group:

```bash
# Get your subscription ID
SUBSCRIPTION_ID=$(az account show --query id --output tsv)

# Assign Contributor role
az role assignment create \
  --assignee 78227685-c5f3-4693-a0fa-2e8d0a711b12 \
  --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-green-squirrel
```

### 4.5: Create Federated Credentials for GitHub

This allows GitHub Actions to authenticate without secrets:

```bash
# Get your GitHub username/org and repo name
GITHUB_ORG="your-github-username-or-org"
GITHUB_REPO="hive-reader-sharp"

# Create federated credential for main branch
az ad app federated-credential create \
  --id 78227685-c5f3-4693-a0fa-2e8d0a711b12 \
  --parameters '{
    "name": "GitHubMainBranch",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"$GITHUB_ORG"'/'"$GITHUB_REPO"':ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for pull requests
az ad app federated-credential create \
  --id 78227685-c5f3-4693-a0fa-2e8d0a711b12 \
  --parameters '{
    "name": "GitHubPullRequests",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"$GITHUB_ORG"'/'"$GITHUB_REPO"':pull_request",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

## Step 5: Configure GitHub Secrets

Add these three secrets to your GitHub repository:

Go to: **Repository Settings → Secrets and variables → Actions → New repository secret**

1. **AZURE_CLIENT_ID**: The `appId` from step 4.2
2. **AZURE_TENANT_ID**: Your tenant ID from step 4.1
3. **AZURE_SUBSCRIPTION_ID**: Your subscription ID from step 4.1

### Using GitHub CLI (alternative)

```bash
# Install GitHub CLI if needed: https://cli.github.com/

gh secret set AZURE_CLIENT_ID --body "78227685-c5f3-4693-a0fa-2e8d0a711b12"
gh secret set AZURE_TENANT_ID --body "<TENANT_ID>"
gh secret set AZURE_SUBSCRIPTION_ID --body "<SUBSCRIPTION_ID>"
```

## Step 6: Test the Deployment

Push a commit to the `main` branch or create a pull request to trigger the GitHub Actions workflow:

```bash
git add .
git commit -m "Deploy to Azure"
git push origin main
```

Monitor the workflow at: `https://github.com/<YOUR_ORG>/hive-reader-sharp/actions`

## Verification

### Check Static Web App Status

```bash
az staticwebapp show \
  --name hive-reader-web-prod \
  --resource-group rg-green-squirrel \
  --query "{name:name, status:status, url:defaultHostname}"
```

### Check Cosmos DB Status

```bash
az cosmosdb show \
  --name hive-reader-db-prod-* \
  --resource-group rg-green-squirrel \
  --query "{name:name, status:provisioningState}"
```

## Troubleshooting

### Workflow fails with "deployment_token was not provided"

- Ensure the Static Web App exists in Azure
- Verify the app name matches in the workflow: `hive-reader-web-prod`
- Check that OIDC federated credentials are set up correctly

### OIDC authentication fails

1. Verify secrets are set correctly in GitHub:

   ```bash
   gh secret list
   ```

2. Check federated credentials:

   ```bash
   az ad app federated-credential list --id 78227685-c5f3-4693-a0fa-2e8d0a711b12
   ```

3. Ensure the service principal has Contributor access:
   ```bash
   az role assignment list \
     --assignee 78227685-c5f3-4693-a0fa-2e8d0a711b12 \
     --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-green-squirrel
   ```

### Deployment succeeds but app doesn't work

1. Check build logs in GitHub Actions
2. Verify the Astro build output location is `dist`
3. Check Static Web App configuration:
   ```bash
   az staticwebapp show \
     --name hive-reader-web-prod \
     --resource-group rg-green-squirrel
   ```

## Environment Variables (Optional)

To connect your Static Web App to Cosmos DB, add application settings:

```bash
# Get Cosmos connection string
COSMOS_CONNECTION=$(az deployment group show \
  --resource-group rg-green-squirrel \
  --name main \
  --query properties.outputs.hiveReaderCosmosConnectionString.value \
  --output tsv)

# Add to Static Web App
az staticwebapp appsettings set \
  --name hive-reader-web-prod \
  --resource-group rg-green-squirrel \
  --setting-names COSMOS_CONNECTION_STRING="$COSMOS_CONNECTION"
```

## Cleanup

To remove all resources:

```bash
az group delete --name rg-green-squirrel --yes --no-wait
```

## Cost Estimation

- **Static Web App (Free tier)**: $0/month
- **Cosmos DB (Serverless + Free tier)**: ~$0-$25/month depending on usage
  - First 1000 RU/s and 25 GB storage free
  - Additional usage billed per request

## Security Best Practices

✅ Using OIDC instead of static credentials
✅ Least privilege access (Contributor only on resource group)
✅ Deployment token automatically masked in logs
✅ No secrets stored in repository

## Next Steps

- [ ] Set up custom domain for Static Web App
- [ ] Configure Azure Front Door for CDN (optional)
- [ ] Set up Application Insights for monitoring
- [ ] Configure Azure Key Vault for sensitive configs

## References

- [Azure Static Web Apps Documentation](https://learn.microsoft.com/en-us/azure/static-web-apps/)
- [GitHub OIDC with Azure](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)
- [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
