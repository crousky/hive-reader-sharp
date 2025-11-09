# Complete setup script for new Azure AD App for GitHub OIDC
# This creates a brand new app named "GH-HiveReader-Deploy"
#
# SAFETY NOTES:
# - This script is NON-DESTRUCTIVE
# - Creates new Azure AD App (does not modify existing apps)
# - Only assigns Contributor role to the specified resource group
# - Does not delete or modify any existing resources

$AppDisplayName = "GH-HiveReader-Deploy"
$GitHubOrg = "crousky"
$GitHubRepo = "hive-reader-sharp"
$ResourceGroup = "rg-green-squirrel"

Write-Host "=== Setting up new Azure AD App for GitHub OIDC ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script will:" -ForegroundColor Yellow
Write-Host "  1. Create a new Azure AD App named '$AppDisplayName'" -ForegroundColor White
Write-Host "  2. Create a Service Principal for the app" -ForegroundColor White
Write-Host "  3. Assign Contributor role ONLY to resource group: $ResourceGroup" -ForegroundColor White
Write-Host "  4. Create federated credentials for GitHub OIDC" -ForegroundColor White
Write-Host ""
Write-Host "This operation is NON-DESTRUCTIVE and will not modify existing resources." -ForegroundColor Green
Write-Host ""

$confirmation = Read-Host "Continue? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# Check if logged in
Write-Host "1. Checking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if ($null -eq $account) {
    Write-Host "  [ERROR] Not logged in to Azure" -ForegroundColor Red
    Write-Host "  Please run: az login" -ForegroundColor Yellow
    exit 1
}
Write-Host "  [OK] Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "  [OK] Subscription: $($account.name)" -ForegroundColor Green

$subscriptionId = $account.id
$tenantId = $account.tenantId

# Create Azure AD App
Write-Host "`n2. Creating Azure AD App '$AppDisplayName'..." -ForegroundColor Yellow
$appJson = az ad app create --display-name $AppDisplayName 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  [ERROR] Failed to create app" -ForegroundColor Red
    Write-Host $appJson -ForegroundColor Red
    exit 1
}

$app = $appJson | ConvertFrom-Json
$appId = $app.appId

Write-Host "  [OK] App created successfully!" -ForegroundColor Green
Write-Host "  App ID (Client ID): $appId" -ForegroundColor Cyan

# Create Service Principal
Write-Host "`n3. Creating Service Principal..." -ForegroundColor Yellow
az ad sp create --id $appId 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Service Principal created" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Failed to create Service Principal" -ForegroundColor Red
    exit 1
}

# Assign Contributor role
Write-Host "`n4. Assigning Contributor role to resource group..." -ForegroundColor Yellow
$roleAssignment = az role assignment create `
    --assignee $appId `
    --role Contributor `
    --scope "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup" 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Contributor role assigned" -ForegroundColor Green
} else {
    Write-Host "  [WARNING] Role assignment may have failed" -ForegroundColor Yellow
    Write-Host "  $roleAssignment" -ForegroundColor Gray
}

# Create federated credentials
Write-Host "`n5. Creating federated credentials..." -ForegroundColor Yellow

# Main branch credential
$mainBranchJson = @"
{
  "name": "GitHubMainBranch",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:$GitHubOrg/$GitHubRepo:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}
"@

$mainBranchFile = [System.IO.Path]::GetTempFileName() + ".json"
$mainBranchJson | Out-File -FilePath $mainBranchFile -Encoding utf8

Write-Host "  Creating credential for main branch..." -ForegroundColor Gray
az ad app federated-credential create --id $appId --parameters "@$mainBranchFile" 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Main branch credential created" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Failed to create main branch credential" -ForegroundColor Red
}
Remove-Item -Path $mainBranchFile -ErrorAction SilentlyContinue

# Pull request credential
$pullRequestJson = @"
{
  "name": "GitHubPullRequests",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:$GitHubOrg/$GitHubRepo:pull_request",
  "audiences": ["api://AzureADTokenExchange"]
}
"@

$pullRequestFile = [System.IO.Path]::GetTempFileName() + ".json"
$pullRequestJson | Out-File -FilePath $pullRequestFile -Encoding utf8

Write-Host "  Creating credential for pull requests..." -ForegroundColor Gray
az ad app federated-credential create --id $appId --parameters "@$pullRequestFile" 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Pull request credential created" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Failed to create pull request credential" -ForegroundColor Red
}
Remove-Item -Path $pullRequestFile -ErrorAction SilentlyContinue

# Verify credentials
Write-Host "`n6. Verifying federated credentials..." -ForegroundColor Yellow
$creds = az ad app federated-credential list --id $appId | ConvertFrom-Json
Write-Host "  [OK] Found $($creds.Count) credential(s)" -ForegroundColor Green
foreach ($cred in $creds) {
    Write-Host "    - $($cred.name): $($cred.subject)" -ForegroundColor Gray
}

# Summary
Write-Host "`n=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Azure AD App Details:" -ForegroundColor Cyan
Write-Host "  App Name: $AppDisplayName" -ForegroundColor White
Write-Host "  App ID (Client ID): $appId" -ForegroundColor White
Write-Host "  Tenant ID: $tenantId" -ForegroundColor White
Write-Host "  Subscription ID: $subscriptionId" -ForegroundColor White
Write-Host ""

Write-Host "GitHub Secrets to Configure:" -ForegroundColor Cyan
Write-Host "  Go to: https://github.com/$GitHubOrg/$GitHubRepo/settings/secrets/actions" -ForegroundColor Gray
Write-Host ""
Write-Host "  AZURE_CLIENT_ID = $appId" -ForegroundColor Yellow
Write-Host "  AZURE_TENANT_ID = $tenantId" -ForegroundColor Yellow
Write-Host "  AZURE_SUBSCRIPTION_ID = $subscriptionId" -ForegroundColor Yellow
Write-Host ""

# Offer to set secrets via GitHub CLI
Write-Host "Set GitHub secrets automatically? (Requires GitHub CLI)" -ForegroundColor Cyan
$response = Read-Host "Type 'yes' to use GitHub CLI, or press Enter to skip"

if ($response -eq "yes") {
    Write-Host "`nSetting GitHub secrets..." -ForegroundColor Yellow

    gh secret set AZURE_CLIENT_ID --body $appId
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] AZURE_CLIENT_ID set" -ForegroundColor Green
    }

    gh secret set AZURE_TENANT_ID --body $tenantId
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] AZURE_TENANT_ID set" -ForegroundColor Green
    }

    gh secret set AZURE_SUBSCRIPTION_ID --body $subscriptionId
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] AZURE_SUBSCRIPTION_ID set" -ForegroundColor Green
    }

    Write-Host "`n[SUCCESS] All GitHub secrets configured!" -ForegroundColor Green
} else {
    Write-Host "`nManually add the secrets above to GitHub" -ForegroundColor Yellow
}

Write-Host "`nNext Steps:" -ForegroundColor Cyan
Write-Host "  1. Ensure GitHub secrets are set (shown above)" -ForegroundColor White
Write-Host "  2. Deploy infrastructure: az deployment group create --resource-group $ResourceGroup --template-file infrastructure/main.bicep --parameters environmentName=prod" -ForegroundColor White
Write-Host "  3. Push to GitHub to trigger deployment" -ForegroundColor White
Write-Host ""
Write-Host "[SUCCESS] Azure AD App setup complete!" -ForegroundColor Green
