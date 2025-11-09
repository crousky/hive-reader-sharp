# Diagnostic script to check Azure AD App and fix federated credentials
$AppId = "78227685-c5f3-4693-a0fa-2e8d0a711b12"

Write-Host "=== Azure AD App Diagnostics ===" -ForegroundColor Cyan
Write-Host ""

# Check if logged in to Azure
Write-Host "1. Checking Azure login status..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if ($null -eq $account) {
    Write-Host "  [ERROR] Not logged in to Azure" -ForegroundColor Red
    Write-Host "  Please run: az login" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "  [OK] Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "  [OK] Subscription: $($account.name) ($($account.id))" -ForegroundColor Green
}

# Check if the app exists
Write-Host "`n2. Checking if Azure AD App exists..." -ForegroundColor Yellow
$app = az ad app show --id $AppId 2>$null | ConvertFrom-Json
if ($null -eq $app) {
    Write-Host "  [ERROR] App with ID $AppId does not exist" -ForegroundColor Red
    Write-Host "`n  Options:" -ForegroundColor Yellow
    Write-Host "  A) Find the correct App ID:" -ForegroundColor White
    Write-Host "     az ad app list --display-name 'GitHub-HiveReader-Deploy'" -ForegroundColor Gray
    Write-Host "`n  B) Create a new app:" -ForegroundColor White
    Write-Host "     az ad app create --display-name 'GitHub-HiveReader-Deploy'" -ForegroundColor Gray

    Write-Host "`n  Searching for apps with 'GitHub' in the name..." -ForegroundColor Yellow
    $apps = az ad app list --filter "startswith(displayName,'GitHub')" 2>$null | ConvertFrom-Json
    if ($apps.Count -gt 0) {
        Write-Host "  [INFO] Found $($apps.Count) app(s):" -ForegroundColor Cyan
        foreach ($a in $apps) {
            Write-Host "    - $($a.displayName) (ID: $($a.appId))" -ForegroundColor White
        }
    } else {
        Write-Host "  [INFO] No apps found starting with 'GitHub'" -ForegroundColor Gray
    }
    exit 1
} else {
    Write-Host "  [OK] App exists: $($app.displayName) (ID: $($app.appId))" -ForegroundColor Green
}

# Check service principal
Write-Host "`n3. Checking if Service Principal exists..." -ForegroundColor Yellow
$sp = az ad sp show --id $AppId 2>$null | ConvertFrom-Json
if ($null -eq $sp) {
    Write-Host "  [WARNING] Service Principal does not exist" -ForegroundColor Yellow
    Write-Host "  Creating Service Principal..." -ForegroundColor Yellow
    az ad sp create --id $AppId 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] Service Principal created successfully" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] Failed to create Service Principal" -ForegroundColor Red
    }
} else {
    Write-Host "  [OK] Service Principal exists" -ForegroundColor Green
}

# List existing federated credentials
Write-Host "`n4. Listing existing federated credentials..." -ForegroundColor Yellow
$creds = az ad app federated-credential list --id $AppId 2>$null | ConvertFrom-Json
if ($creds.Count -eq 0) {
    Write-Host "  [INFO] No federated credentials found" -ForegroundColor Gray
} else {
    Write-Host "  [INFO] Found $($creds.Count) credential(s):" -ForegroundColor Cyan
    foreach ($cred in $creds) {
        Write-Host "    - Name: $($cred.name)" -ForegroundColor White
        Write-Host "      Subject: $($cred.subject)" -ForegroundColor Gray
        Write-Host "      Issuer: $($cred.issuer)" -ForegroundColor Gray
    }
}

# Check role assignments
Write-Host "`n5. Checking role assignments..." -ForegroundColor Yellow
$roles = az role assignment list --assignee $AppId 2>$null | ConvertFrom-Json
if ($roles.Count -eq 0) {
    Write-Host "  [WARNING] No role assignments found" -ForegroundColor Yellow
    Write-Host "  You may need to assign Contributor role to the resource group" -ForegroundColor Yellow
} else {
    Write-Host "  [OK] Found $($roles.Count) role assignment(s):" -ForegroundColor Green
    foreach ($role in $roles) {
        Write-Host "    - Role: $($role.roleDefinitionName)" -ForegroundColor White
        Write-Host "      Scope: $($role.scope)" -ForegroundColor Gray
    }
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "App ID: $AppId" -ForegroundColor White
Write-Host "App exists: $(if ($null -ne $app) { 'Yes' } else { 'No' })" -ForegroundColor White
Write-Host "Service Principal exists: $(if ($null -ne $sp) { 'Yes' } else { 'No' })" -ForegroundColor White
Write-Host "Federated credentials: $($creds.Count)" -ForegroundColor White
Write-Host "Role assignments: $($roles.Count)" -ForegroundColor White

Write-Host "`nNext steps:" -ForegroundColor Cyan
if ($null -ne $app -and $null -ne $sp) {
    Write-Host "  Run: .\FIX_FEDERATED_CREDENTIALS.ps1" -ForegroundColor Green
} else {
    Write-Host "  Fix the issues above first" -ForegroundColor Yellow
}
