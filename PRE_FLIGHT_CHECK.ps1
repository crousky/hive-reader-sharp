# Pre-flight check script before pushing to GitHub
# Verifies that everything is configured correctly for deployment

Write-Host "=== Pre-Flight Check for Azure Deployment ===" -ForegroundColor Cyan
Write-Host ""

$allGood = $true

# 1. Check Azure CLI is installed
Write-Host "1. Checking Azure CLI..." -ForegroundColor Yellow
$azVersion = az version 2>$null
if ($null -eq $azVersion) {
    Write-Host "  [FAIL] Azure CLI is not installed" -ForegroundColor Red
    Write-Host "  Install from: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Gray
    $allGood = $false
} else {
    Write-Host "  [OK] Azure CLI is installed" -ForegroundColor Green
}

# 2. Check Azure login
Write-Host "`n2. Checking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if ($null -eq $account) {
    Write-Host "  [FAIL] Not logged in to Azure" -ForegroundColor Red
    Write-Host "  Run: az login" -ForegroundColor Gray
    $allGood = $false
} else {
    Write-Host "  [OK] Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "  [OK] Subscription: $($account.name)" -ForegroundColor Green
}

# 3. Check resource group exists
Write-Host "`n3. Checking resource group..." -ForegroundColor Yellow
$rg = az group show --name rg-green-squirrel 2>$null | ConvertFrom-Json
if ($null -eq $rg) {
    Write-Host "  [FAIL] Resource group 'rg-green-squirrel' does not exist" -ForegroundColor Red
    Write-Host "  Create it: az group create --name rg-green-squirrel --location eastus2" -ForegroundColor Gray
    $allGood = $false
} else {
    Write-Host "  [OK] Resource group exists: rg-green-squirrel" -ForegroundColor Green
    Write-Host "  [OK] Location: $($rg.location)" -ForegroundColor Green
}

# 4. Check GitHub CLI (optional but helpful)
Write-Host "`n4. Checking GitHub CLI (optional)..." -ForegroundColor Yellow
$ghVersion = gh --version 2>$null
if ($null -eq $ghVersion) {
    Write-Host "  [INFO] GitHub CLI is not installed (optional)" -ForegroundColor Gray
    Write-Host "  Install from: https://cli.github.com/" -ForegroundColor Gray
} else {
    Write-Host "  [OK] GitHub CLI is installed" -ForegroundColor Green
}

# 5. Check GitHub secrets
Write-Host "`n5. Checking GitHub secrets..." -ForegroundColor Yellow
if ($null -ne $ghVersion) {
    $secrets = gh secret list 2>$null
    if ($LASTEXITCODE -eq 0) {
        $requiredSecrets = @('AZURE_CLIENT_ID', 'AZURE_TENANT_ID', 'AZURE_SUBSCRIPTION_ID')
        $missingSecrets = @()

        foreach ($secret in $requiredSecrets) {
            if ($secrets -notmatch $secret) {
                $missingSecrets += $secret
            }
        }

        if ($missingSecrets.Count -eq 0) {
            Write-Host "  [OK] All required GitHub secrets are set" -ForegroundColor Green
        } else {
            Write-Host "  [FAIL] Missing GitHub secrets: $($missingSecrets -join ', ')" -ForegroundColor Red
            Write-Host "  Run: .\SETUP_NEW_AZURE_APP.ps1" -ForegroundColor Gray
            $allGood = $false
        }
    } else {
        Write-Host "  [WARN] Could not check GitHub secrets (not in a git repository or not authenticated)" -ForegroundColor Yellow
        Write-Host "  Ensure these secrets are set in GitHub:" -ForegroundColor Gray
        Write-Host "    - AZURE_CLIENT_ID" -ForegroundColor Gray
        Write-Host "    - AZURE_TENANT_ID" -ForegroundColor Gray
        Write-Host "    - AZURE_SUBSCRIPTION_ID" -ForegroundColor Gray
    }
} else {
    Write-Host "  [SKIP] Cannot check secrets without GitHub CLI" -ForegroundColor Gray
    Write-Host "  Manually verify these secrets are set in GitHub:" -ForegroundColor Gray
    Write-Host "    - AZURE_CLIENT_ID" -ForegroundColor Gray
    Write-Host "    - AZURE_TENANT_ID" -ForegroundColor Gray
    Write-Host "    - AZURE_SUBSCRIPTION_ID" -ForegroundColor Gray
}

# 6. Check bicep files exist
Write-Host "`n6. Checking bicep templates..." -ForegroundColor Yellow
$bicepFiles = @(
    "infrastructure/main.bicep",
    "infrastructure/static-web-app.bicep",
    "infrastructure/hive-reader-cosmos.bicep"
)

$missingFiles = @()
foreach ($file in $bicepFiles) {
    if (-not (Test-Path $file)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -eq 0) {
    Write-Host "  [OK] All bicep templates found" -ForegroundColor Green
} else {
    Write-Host "  [FAIL] Missing bicep files: $($missingFiles -join ', ')" -ForegroundColor Red
    $allGood = $false
}

# 7. Check GitHub Actions workflow
Write-Host "`n7. Checking GitHub Actions workflow..." -ForegroundColor Yellow
if (Test-Path ".github/workflows/azure-static-web-app.yml") {
    Write-Host "  [OK] GitHub Actions workflow found" -ForegroundColor Green
} else {
    Write-Host "  [FAIL] GitHub Actions workflow not found" -ForegroundColor Red
    $allGood = $false
}

# 8. Check web application exists
Write-Host "`n8. Checking web application..." -ForegroundColor Yellow
if (Test-Path "web/package.json") {
    Write-Host "  [OK] Web application found" -ForegroundColor Green
} else {
    Write-Host "  [WARN] web/package.json not found" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan

if ($allGood) {
    Write-Host "`n[SUCCESS] All pre-flight checks passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You are ready to deploy! The GitHub Action will:" -ForegroundColor Cyan
    Write-Host "  1. Automatically deploy infrastructure (non-destructive)" -ForegroundColor White
    Write-Host "  2. Create resources if they don't exist" -ForegroundColor White
    Write-Host "  3. Update existing resources" -ForegroundColor White
    Write-Host "  4. Deploy your web application" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  git add ." -ForegroundColor White
    Write-Host "  git commit -m 'Configure Azure deployment'" -ForegroundColor White
    Write-Host "  git push origin main" -ForegroundColor White
    Write-Host ""
    Write-Host "Or create a pull request to test the deployment in a PR environment." -ForegroundColor Gray
} else {
    Write-Host "`n[FAIL] Some checks failed. Please fix the issues above before deploying." -ForegroundColor Red
    Write-Host ""
    Write-Host "Common fixes:" -ForegroundColor Yellow
    Write-Host "  1. Run: .\SETUP_NEW_AZURE_APP.ps1  (to set up Azure AD App and GitHub secrets)" -ForegroundColor White
    Write-Host "  2. Run: az login  (to login to Azure)" -ForegroundColor White
    Write-Host "  3. Create resource group if needed" -ForegroundColor White
    Write-Host ""
    Write-Host "After fixing, run this script again: .\PRE_FLIGHT_CHECK.ps1" -ForegroundColor Cyan
}

Write-Host ""
