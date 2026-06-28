<#
.SYNOPSIS
  One-time setup so a GitHub Actions pipeline may deploy to your Azure subscription via
  OIDC (no stored Azure password).

.DESCRIPTION
  Creates an Entra (Azure AD) app registration + service principal, adds a federated
  credential that trusts your GitHub repo's `main` branch, and grants it a role on the
  subscription. Prints the GitHub secrets/variables to set afterwards.

  Run this once, signed in as an account that can create app registrations and assign
  roles on the subscription (Owner, or Contributor + User Access Administrator).

.EXAMPLE
  ./infra/setup-github-oidc.ps1 -GitHubRepo "andree-antfolk/StuffInABox" -SubscriptionId "<sub-guid>"

.PARAMETER IncludeUserAccessAdmin
  Also grant "User Access Administrator" — required only if you enable the Key Vault
  module (it creates a role assignment, which needs this permission).
#>
param(
  [Parameter(Mandatory)] [string]$GitHubRepo,            # e.g. "owner/repo"
  [Parameter(Mandatory)] [string]$SubscriptionId,
  [string]$Branch = 'main',
  [string]$AppName = 'stuffinabox-github-deploy',
  [switch]$IncludeUserAccessAdmin
)

$ErrorActionPreference = 'Stop'
az account set --subscription $SubscriptionId | Out-Null
$tenantId = (az account show --query tenantId -o tsv)

Write-Host "Creating/looking up Entra app '$AppName'..." -ForegroundColor Cyan
$appId = az ad app list --display-name $AppName --query "[0].appId" -o tsv
if (-not $appId) {
  $appId = az ad app create --display-name $AppName --query appId -o tsv
}
# Ensure a service principal exists for the app.
if (-not (az ad sp show --id $appId 2>$null)) {
  az ad sp create --id $appId | Out-Null
}

Write-Host "Adding federated credential for repo '$GitHubRepo' (branch '$Branch')..." -ForegroundColor Cyan
$fcName = "github-$Branch"
$exists = az ad app federated-credential list --id $appId --query "[?name=='$fcName'] | [0].name" -o tsv
if (-not $exists) {
  $fc = @{
    name      = $fcName
    issuer    = 'https://token.actions.githubusercontent.com'
    subject   = "repo:${GitHubRepo}:ref:refs/heads/$Branch"
    audiences = @('api://AzureADTokenExchange')
  } | ConvertTo-Json -Compress
  $tmp = New-TemporaryFile
  Set-Content -Path $tmp -Value $fc -Encoding utf8
  az ad app federated-credential create --id $appId --parameters $tmp | Out-Null
  Remove-Item $tmp
}

Write-Host "Assigning roles on the subscription..." -ForegroundColor Cyan
$scope = "/subscriptions/$SubscriptionId"
az role assignment create --assignee $appId --role 'Contributor' --scope $scope | Out-Null
if ($IncludeUserAccessAdmin) {
  az role assignment create --assignee $appId --role 'User Access Administrator' --scope $scope | Out-Null
}

Write-Host ''
Write-Host '=== Done. Set these in GitHub (repo -> Settings -> Secrets and variables -> Actions) ===' -ForegroundColor Green
Write-Host ''
Write-Host 'App secrets live in Key Vault (Db-Connection, Jwt-Secret, Email-Smtp-Password,' -ForegroundColor DarkGray
Write-Host 'OAuth-Google-ClientSecret, OAuth-Microsoft-ClientSecret) - the pipeline never sees them.' -ForegroundColor DarkGray
Write-Host ''
Write-Host 'Secrets (only the OIDC identifiers):' -ForegroundColor Yellow
Write-Host "  AZURE_CLIENT_ID        = $appId"
Write-Host "  AZURE_TENANT_ID        = $tenantId"
Write-Host "  AZURE_SUBSCRIPTION_ID  = $SubscriptionId"
Write-Host ''
Write-Host 'Variables (non-secret):' -ForegroundColor Yellow
Write-Host '  AZURE_LOCATION          = swedencentral'
Write-Host '  AZURE_ENV               = prod'
Write-Host '  AZURE_APP_NAME          = <globally-unique, e.g. stuffinabox-andree>'
Write-Host '  AZURE_APP_SKU           = F1'
Write-Host '  AZURE_KEY_VAULT         = kv-stuffinabox-andree'
Write-Host '  SIB_BREVO_USER          = b0375d001@smtp-brevo.com'
Write-Host '  SIB_EMAIL_FROM          = andree.antfolk@gmail.com'
Write-Host '  SIB_GOOGLE_CLIENT_ID    = <google client id (public)>'
Write-Host '  SIB_MICROSOFT_CLIENT_ID = <microsoft client id (public)>'
