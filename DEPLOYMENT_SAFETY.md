# Deployment Safety Guide

## Overview

All deployment scripts and Bicep templates in this repository are designed to be **NON-DESTRUCTIVE**. This document explains the safety mechanisms in place.

## What "Non-Destructive" Means

### Bicep Deployments

All Bicep deployments use **Incremental mode** (Azure's default):

```bash
az deployment group create \
  --resource-group rg-green-squirrel \
  --template-file infrastructure/main.bicep \
  --parameters environmentName=prod \
  --mode Incremental  # This is the default
```

#### Incremental Mode Behavior:

✅ **Creates** new resources that don't exist
✅ **Updates** existing resources to match the template definition
✅ **Preserves** all data in existing resources (databases, storage, etc.)
✅ **Leaves untouched** any resources not defined in the template
❌ **Never deletes** resources from the resource group

#### What Gets Updated vs. Preserved:

| Resource Type | What Gets Updated | What Gets Preserved |
|---------------|-------------------|---------------------|
| **Static Web App** | SKU, build configuration | Custom domains, SSL certs, app settings |
| **Cosmos DB** | Consistency level, locations | **All data**, throughput settings, containers |
| **Cosmos Containers** | Indexing policy, partition key | **All documents/data** |

### Complete Mode (NOT USED)

We **never** use Complete mode, which would delete resources not in the template:

```bash
# ⚠️ DANGEROUS - We never use this!
az deployment group create \
  --mode Complete  # DON'T DO THIS
```

## Resource Group Management

### Pre-Existing Resource Group

Your resource group `rg-green-squirrel` already exists and **will never be deleted** by our scripts.

The Bicep templates:
- ✅ Deploy resources **into** the existing resource group
- ❌ Do **not** create or delete the resource group
- ❌ Do **not** modify resource group tags or locks

## Safety Mechanisms by Component

### 1. Cosmos DB (hive-reader-cosmos.bicep)

**Safe Operations:**
- Creates database if it doesn't exist
- Creates containers if they don't exist
- Updates indexing policies

**Data Safety:**
- ✅ **Never deletes data** from containers
- ✅ **Never drops databases or containers**
- ✅ Preserves all documents during updates
- ✅ Partition key changes are **blocked** by Azure (prevents data loss)

**Example - Re-running deployment:**
```bash
# First deployment: Creates database + containers
az deployment group create --template-file infrastructure/main.bicep ...

# Second deployment: Updates definitions, preserves all data
az deployment group create --template-file infrastructure/main.bicep ...
```

### 2. Static Web App (static-web-app.bicep)

**Safe Operations:**
- Creates Static Web App if it doesn't exist
- Updates build configuration
- Updates SKU (Free/Standard)

**Configuration Safety:**
- ✅ Preserves custom domains
- ✅ Preserves SSL certificates
- ✅ Preserves application settings
- ✅ Preserves authentication providers
- ✅ Preserves deployment history

### 3. Azure AD App (SETUP_NEW_AZURE_APP.ps1)

**Safe Operations:**
- Creates a **new** Azure AD App
- Creates Service Principal
- Assigns Contributor role **only** to specified resource group
- Creates federated credentials

**Safety Features:**
- ✅ Creates new app (doesn't modify existing apps)
- ✅ Only assigns permissions to `rg-green-squirrel`
- ✅ Doesn't delete or modify existing apps
- ✅ Requires explicit confirmation before proceeding

## Verification Before Deployment

### Check What Will Be Deployed

Use the "what-if" operation to preview changes:

```bash
az deployment group what-if \
  --resource-group rg-green-squirrel \
  --template-file infrastructure/main.bicep \
  --parameters environmentName=prod
```

This shows:
- ✅ Resources that will be created (green)
- ✅ Resources that will be modified (yellow)
- ✅ Resources that will be ignored (gray)
- ❌ No resources will be deleted (in Incremental mode)

### Example What-If Output

```
Resource changes: 3 to create, 1 to modify, 0 to delete, 5 to ignore

  + Microsoft.Web/staticSites/hive-reader-web-prod
  + Microsoft.DocumentDB/databaseAccounts/hive-reader-db-prod-xyz
  + Microsoft.DocumentDB/databaseAccounts/hive-reader-db-prod-xyz/sqlDatabases/HiveReaderDB

  ~ Microsoft.Web/staticSites/hive-reader-web-prod (existing)
    ~ properties.buildProperties.outputLocation: "build" => "dist"
```

## Rollback Safety

### If Something Goes Wrong

Azure deployments are atomic per resource:
- ✅ If a resource fails to deploy, it rolls back that resource
- ✅ Other successful resources remain deployed
- ✅ No data is lost during rollback
- ✅ Existing resources remain functional

### Manual Rollback

If you need to revert changes:

```bash
# Re-deploy the previous template version
az deployment group create \
  --resource-group rg-green-squirrel \
  --template-file infrastructure/main.bicep.backup \
  --parameters environmentName=prod
```

## Data Backup Recommendations

While deployments are non-destructive, we recommend:

### Cosmos DB Backups

Automatic backups are enabled (configured in template):
- ✅ Backup interval: 240 minutes (4 hours)
- ✅ Backup retention: 8 hours
- ✅ Backup redundancy: Local

To restore from backup if needed:
```bash
az cosmosdb sql database restore \
  --account-name hive-reader-db-prod-{suffix} \
  --resource-group rg-green-squirrel \
  --name HiveReaderDB \
  --restore-timestamp "2025-01-09T12:00:00Z"
```

## Dangerous Operations (Explicitly Blocked)

These operations are **never** performed by our scripts:

❌ `az deployment group create --mode Complete`
❌ `az group delete`
❌ `az cosmosdb delete`
❌ `az cosmosdb sql database delete`
❌ `az cosmosdb sql container delete`
❌ `az staticwebapp delete`
❌ Modifying partition keys on existing containers
❌ Changing Cosmos DB account type (serverless → provisioned)

## Emergency Contact

If you need to perform any destructive operation:

1. **Stop** - Do not proceed
2. **Backup** - Ensure all data is backed up
3. **Review** - Understand the impact
4. **Document** - Record why the operation is needed
5. **Execute** - Run the operation manually with explicit flags

## Summary

✅ **Safe by Default:** All scripts use non-destructive modes
✅ **Data Preserved:** Database data is never deleted
✅ **Incremental Updates:** Only creates or updates resources
✅ **No Deletions:** Resources are never automatically deleted
✅ **Existing RG:** Uses existing `rg-green-squirrel` resource group
✅ **Confirmation Required:** Scripts require explicit user confirmation
✅ **What-If Available:** Preview changes before deploying

You can safely run deployments multiple times without risk of data loss.
