# PowerShell Scripts Guide

This directory contains PowerShell automation scripts for Azure deployment setup and troubleshooting.

## Security Notice

⚠️ **Important**: These scripts have been parameterized to avoid storing sensitive Azure AD App IDs in the repository. Always pass your App ID as a parameter rather than hardcoding it.

## Available Scripts

### 1. SETUP_NEW_AZURE_APP.ps1

Creates a new Azure AD App for GitHub OIDC authentication.

**Usage:**
```powershell
.\SETUP_NEW_AZURE_APP.ps1
```

**What it does:**
- Creates Azure AD App named "GH-HiveReader-Deploy"
- Creates Service Principal
- Assigns Contributor role to resource group
- Sets up federated credentials for GitHub OIDC
- Optionally configures GitHub secrets via GitHub CLI

**Security:** This script creates a NEW app, so no sensitive IDs need to be provided upfront.

---

### 2. PRE_FLIGHT_CHECK.ps1

Validates your environment is ready for deployment.

**Usage:**
```powershell
.\PRE_FLIGHT_CHECK.ps1
```

**Checks:**
- Azure CLI installed and logged in
- Resource group exists
- GitHub CLI installed (optional)
- GitHub secrets configured
- Bicep templates present
- GitHub Actions workflow exists

**Security:** Read-only script, no sensitive parameters required.

---

### 3. FIX_FEDERATED_CREDENTIALS.ps1

Fixes or updates federated credentials for an existing Azure AD App.

**Usage:**
```powershell
# Basic usage with your App ID
.\FIX_FEDERATED_CREDENTIALS.ps1 -AppId "YOUR-APP-ID-HERE"

# With custom GitHub org/repo
.\FIX_FEDERATED_CREDENTIALS.ps1 -AppId "YOUR-APP-ID-HERE" -GitHubOrg "yourorg" -GitHubRepo "yourrepo"
```

**Parameters:**
- `-AppId` (required): Your Azure AD App ID (Client ID)
- `-GitHubOrg` (optional): GitHub organization name (default: "crousky")
- `-GitHubRepo` (optional): GitHub repository name (default: "hive-reader-sharp")

**What it does:**
- Lists current federated credentials
- Deletes existing credentials
- Creates new credentials with correct subjects
- Verifies the new credentials

**Security:** Requires your App ID as a parameter. Never commit this value to the repository.

**Example:**
```powershell
# Get your App ID first
az ad app list --display-name "GH-HiveReader-Deploy" --query "[].appId" -o tsv

# Then run the script
.\FIX_FEDERATED_CREDENTIALS.ps1 -AppId "51924043-69ff-46d3-a287-e891b7261236"
```

---

### 4. DIAGNOSE_AZURE_APP.ps1

Diagnoses issues with your Azure AD App and federated credentials.

**Usage:**
```powershell
.\DIAGNOSE_AZURE_APP.ps1 -AppId "YOUR-APP-ID-HERE"
```

**Parameters:**
- `-AppId` (required): Your Azure AD App ID (Client ID)

**What it checks:**
- Azure login status
- App existence
- Federated credentials (validates subjects)
- Service principal status
- Role assignments

**Security:** Requires your App ID as a parameter. Never commit this value to the repository.

---

## Finding Your App ID

If you don't know your App ID, run:

```powershell
# List all apps
az ad app list --query "[].{displayName:displayName, appId:appId}" --output table

# Find specific app
az ad app list --display-name "GH-HiveReader-Deploy" --query "[].appId" -o tsv
```

## Security Best Practices

1. **Never commit App IDs to public repositories** - While not as sensitive as secrets, App IDs should not be exposed publicly
2. **Use parameters** - All scripts that need App IDs accept them as parameters
3. **Store locally** - Keep your App ID in a local file or environment variable:
   ```powershell
   # Store in environment variable
   $env:AZURE_APP_ID = "your-app-id-here"

   # Use in scripts
   .\FIX_FEDERATED_CREDENTIALS.ps1 -AppId $env:AZURE_APP_ID
   ```
4. **Use GitHub secrets for CI/CD** - The GitHub Actions workflow uses secrets, not hardcoded values

## Troubleshooting

### "App with ID does not exist"

The App ID you provided is incorrect or the app was deleted. List your apps:
```powershell
az ad app list --query "[].{displayName:displayName, appId:appId}" --output table
```

### "Not logged in to Azure"

Run:
```powershell
az login
```

### "No matching federated identity record found"

This is why you need the FIX_FEDERATED_CREDENTIALS.ps1 script. Run it with your App ID:
```powershell
.\FIX_FEDERATED_CREDENTIALS.ps1 -AppId "YOUR-APP-ID"
```

### "AZURE_CLIENT_ID secret not set in GitHub"

Either:
1. Run SETUP_NEW_AZURE_APP.ps1 and choose "yes" to set secrets automatically
2. Manually set secrets in GitHub: Settings → Secrets and variables → Actions

## Next Steps

After running these scripts successfully:

1. Verify with PRE_FLIGHT_CHECK.ps1
2. Push to GitHub to trigger deployment
3. Monitor GitHub Actions at: https://github.com/crousky/hive-reader-sharp/actions

## Support

For issues or questions, see:
- [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md) - Complete deployment guide
- [DEPLOYMENT_SAFETY.md](DEPLOYMENT_SAFETY.md) - Safety documentation
