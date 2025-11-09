# PowerShell script to fix federated credentials for GitHub OIDC authentication
# Run this script to update the federated credentials with the correct repository path
#
# Usage:
#   .\FIX_FEDERATED_CREDENTIALS.ps1 -AppId <your-app-id>
#   .\FIX_FEDERATED_CREDENTIALS.ps1 -AppId <your-app-id> -GitHubOrg <org> -GitHubRepo <repo>

param(
    [Parameter(Mandatory=$true, HelpMessage="Azure AD App ID (Client ID)")]
    [string]$AppId,

    [Parameter(Mandatory=$false, HelpMessage="GitHub organization name")]
    [string]$GitHubOrg = "crousky",

    [Parameter(Mandatory=$false, HelpMessage="GitHub repository name")]
    [string]$GitHubRepo = "hive-reader-sharp"
)

Write-Host "Fixing federated credentials for GitHub OIDC authentication..." -ForegroundColor Cyan

# Check existing federated credentials
Write-Host "`nListing existing federated credentials..." -ForegroundColor Yellow
az ad app federated-credential list --id $AppId

# Delete existing credentials (if they exist)
Write-Host "`nDeleting existing federated credentials..." -ForegroundColor Yellow

Write-Host "  Attempting to delete GitHubPullRequests credential..." -ForegroundColor Gray
az ad app federated-credential delete --id $AppId --federated-credential-id "GitHubPullRequests" 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Deleted GitHubPullRequests credential" -ForegroundColor Green
} else {
    Write-Host "  [INFO] GitHubPullRequests credential not found (this is okay)" -ForegroundColor Gray
}

Write-Host "  Attempting to delete GitHubMainBranch credential..." -ForegroundColor Gray
az ad app federated-credential delete --id $AppId --federated-credential-id "GitHubMainBranch" 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Deleted GitHubMainBranch credential" -ForegroundColor Green
} else {
    Write-Host "  [INFO] GitHubMainBranch credential not found (this is okay)" -ForegroundColor Gray
}

# Create the correct federated credential for main branch
Write-Host "`nCreating federated credential for main branch..." -ForegroundColor Yellow

# Create JSON object with proper string interpolation
$mainBranchSubject = "repo:${GitHubOrg}/${GitHubRepo}:ref:refs/heads/main"
$mainBranchJson = @{
    name = "GitHubMainBranch"
    issuer = "https://token.actions.githubusercontent.com"
    subject = $mainBranchSubject
    audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json -Compress

Write-Host "  Debug - Main branch subject: $mainBranchSubject" -ForegroundColor Gray

$mainBranchFile = [System.IO.Path]::GetTempFileName()
[System.IO.File]::WriteAllText($mainBranchFile, $mainBranchJson, [System.Text.Encoding]::UTF8)

az ad app federated-credential create --id $AppId --parameters "@$mainBranchFile"

if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Created GitHubMainBranch credential successfully" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Failed to create GitHubMainBranch credential" -ForegroundColor Red
    Remove-Item -Path $mainBranchFile -ErrorAction SilentlyContinue
    exit 1
}

Remove-Item -Path $mainBranchFile -ErrorAction SilentlyContinue

# Create the correct federated credential for pull requests
Write-Host "`nCreating federated credential for pull requests..." -ForegroundColor Yellow

# Create JSON object with proper string interpolation
$pullRequestSubject = "repo:${GitHubOrg}/${GitHubRepo}:pull_request"
$pullRequestJson = @{
    name = "GitHubPullRequests"
    issuer = "https://token.actions.githubusercontent.com"
    subject = $pullRequestSubject
    audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json -Compress

Write-Host "  Debug - Pull request subject: $pullRequestSubject" -ForegroundColor Gray

$pullRequestFile = [System.IO.Path]::GetTempFileName()
[System.IO.File]::WriteAllText($pullRequestFile, $pullRequestJson, [System.Text.Encoding]::UTF8)

az ad app federated-credential create --id $AppId --parameters "@$pullRequestFile"

if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Created GitHubPullRequests credential successfully" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Failed to create GitHubPullRequests credential" -ForegroundColor Red
    Remove-Item -Path $pullRequestFile -ErrorAction SilentlyContinue
    exit 1
}

Remove-Item -Path $pullRequestFile -ErrorAction SilentlyContinue

# Verify the new credentials
Write-Host "`nVerifying federated credentials..." -ForegroundColor Yellow
az ad app federated-credential list --id $AppId

Write-Host "`n[SUCCESS] Federated credentials have been updated successfully!" -ForegroundColor Green
Write-Host "`nYou can now re-run your GitHub Actions workflow." -ForegroundColor Cyan
