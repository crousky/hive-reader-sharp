# Infrastructure as Code

This directory contains Azure Bicep templates for deploying the application's infrastructure.

## Files

- `main.bicep` - Main deployment template that orchestrates all resources
- `cosmos.bicep` - Cosmos DB account, database, and containers configuration

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- An Azure subscription
- Appropriate permissions to create resources in Azure

## Deployment

### 1. Login to Azure

```bash
az login
```

### 2. Create a Resource Group

```bash
az group create --name rg-sendtokindle-dev --location eastus
```

### 3. Deploy the Infrastructure

```bash
az deployment group create \
  --resource-group rg-sendtokindle-dev \
  --template-file infrastructure/main.bicep \
  --parameters environmentName=dev \
  --parameters location=eastus
```

### 4. Get the Cosmos DB Connection Information

```bash
# Get the endpoint
az deployment group show \
  --resource-group rg-sendtokindle-dev \
  --name main \
  --query properties.outputs.cosmosEndpoint.value

# Get the primary key
COSMOS_ACCOUNT=$(az deployment group show \
  --resource-group rg-sendtokindle-dev \
  --name main \
  --query properties.outputs.cosmosAccountName.value -o tsv)

az cosmosdb keys list \
  --name $COSMOS_ACCOUNT \
  --resource-group rg-sendtokindle-dev \
  --query primaryMasterKey -o tsv
```

## Resources Created

The deployment creates the following resources:

### Cosmos DB Account
- **Tier**: Serverless (pay-per-request)
- **Free Tier**: Enabled (first 1000 RU/s and 25 GB storage free)
- **Consistency**: Session level
- **Backup**: Periodic (every 4 hours, retained for 8 hours)

### Database: SendToKindleDB

### Container: Users
- **Partition Key**: `/id`
- **TTL**: None (users persist indefinitely)
- **Purpose**: Store user account information

### Container: Sessions
- **Partition Key**: `/userId`
- **TTL**: 86400 seconds (24 hours)
- **Purpose**: Store user sessions with automatic expiration

## Cost Estimate

With the serverless tier and free tier enabled:
- First 1000 RU/s and 25 GB storage are free each month
- After free tier: ~$0.25 per million read operations, ~$1.25 per million write operations
- Storage: ~$0.25 per GB per month

For a small to medium application, this typically costs less than $5-10 per month.

## Cleanup

To delete all resources:

```bash
az group delete --name rg-sendtokindle-dev --yes --no-wait
```
