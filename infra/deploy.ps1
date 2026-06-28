<#
.SYNOPSIS
  Deploys the StuffInABox Azure infrastructure (subscription-scoped Bicep).
.DESCRIPTION
  Validates Azure login, then runs the deployment with infra/main.bicepparam.
  Secrets are read from SIB_* environment variables (see infra/README.md) — set them
  before running. Use -WhatIf for a preview without changing anything.
.EXAMPLE
  ./infra/deploy.ps1 -Location swedencentral
.EXAMPLE
  ./infra/deploy.ps1 -WhatIf
#>
param(
  [string]$Location = 'swedencentral',
  [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
$infra = $PSScriptRoot

# Ensure we're logged in.
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
  Write-Host 'Not logged in — running az login...' -ForegroundColor Yellow
  az login | Out-Null
  $account = az account show | ConvertFrom-Json
}
Write-Host "Subscription: $($account.name) ($($account.id))" -ForegroundColor Cyan

# Secrets are not passed here — they live in Key Vault (see infra/README.md). This deploy
# only sets non-secret config and the Key Vault references.

$args = @(
  'deployment', 'sub', 'create',
  '-l', $Location,
  '-f', (Join-Path $infra 'main.bicep'),
  '-p', (Join-Path $infra 'main.bicepparam')
)
if ($WhatIf) { $args += '--what-if' }

Write-Host "Running: az $($args -join ' ')" -ForegroundColor Cyan
az @args
