// App Service plan + Linux Web App that hosts the ADMIN application (StuffInABox.Admin.Web).
//
// This is a SEPARATE host from the consumer web app (its own plan + site) so it has its own
// public surface and its own deploy — matching the "isolated admin app, shared core" design.
// It talks directly to the same database (shared AppDbContext) and signs users in with Entra
// ID (single-tenant, ID-token-only flow → NO client secret). The only secret it needs is the
// DB connection string, wired in as a Key Vault reference read by its managed identity.
//
// Prerequisite: the Key Vault named by `keyVaultName` must hold the `Db-Connection` secret,
// and this app's managed identity must have "Key Vault Secrets User" on it (main.bicep wires
// that up via keyVault.bicep when both enableKeyVault and enableAdminApp are on).
//
// ─────────────────────────────────────────────────────────────────────────────────────────
// HARDENING — not enabled here to keep the first cut simple. When the admin app is exposed
// to the public internet, consider (roughly in order of value):
//   1. Access restrictions (IP allow-list) on the site — lock it to your office/VPN egress
//      IPs so the admin surface isn't reachable from the open internet at all. One-liner:
//      add `ipSecurityRestrictions` to `siteConfig`, or `az webapp config access-restriction add`.
//   2. Tighten "who is an admin". Today ANY user in the tenant can sign in and is treated as
//      admin. Add an app-role / group check (or an explicit allow-list of object-ids) so a new
//      tenant guest can't self-serve admin.
//   3. Private networking: VNet integration + Private Endpoint on the Key Vault (and DB), so
//      secrets/DB never traverse the public internet. Needs a Basic+ plan.
//   4. Front with a WAF (Azure Front Door / App Gateway) for rate-limiting + bot protection.
//   5. Diagnostics: send app + auth logs to Log Analytics and alert on failed sign-ins.
//   6. Dedicated least-privilege Key Vault (only `Db-Connection`) instead of sharing the
//      consumer app's vault, so a compromised admin identity can't read OAuth/JWT secrets.
// ─────────────────────────────────────────────────────────────────────────────────────────

@description('Azure region for the resources.')
param location string

@description('Globally-unique admin web app name (becomes <name>.azurewebsites.net).')
param adminAppName string

@description('App Service plan SKU. F1 = Free (good for testing), B1 = Basic (~$13/mo), etc.')
param skuName string = 'F1'

@description('Linux runtime stack for the web app.')
param linuxFxVersion string = 'DOTNETCORE|10.0'

@description('EF database provider: "postgres" (Supabase/Azure) or "sqlite".')
param databaseProvider string = 'postgres'

@description('Name of the Key Vault holding the DB connection secret.')
param keyVaultName string

@description('Entra ID (Azure AD) authority instance. Defaults to the current cloud login endpoint.')
param azureAdInstance string = environment().authentication.loginEndpoint

@description('Entra tenant id the admin sign-in is locked to (single-tenant).')
param azureAdTenantId string

@description('Entra app-registration client id for the admin app (output of entra/admin-app.bicep).')
param azureAdClientId string

// Free/Shared tiers don't support Always On; Basic and up do.
var supportsAlwaysOn = !contains(['F1', 'FREE', 'D1', 'SHARED'], toUpper(skuName))

// Builds a Key Vault reference (latest version) for an app setting value.
func kvRef(vault string, secret string) string =>
  '@Microsoft.KeyVault(VaultName=${vault};SecretName=${secret})'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'plan-${adminAppName}'
  location: location
  kind: 'linux'
  sku: {
    name: skuName
  }
  properties: {
    reserved: true // required for Linux
  }
}

resource site 'Microsoft.Web/sites@2023-12-01' = {
  name: adminAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned' // used to read the Key Vault reference below
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      alwaysOn: supportsAlwaysOn
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'Database__Provider', value: databaseProvider }
        // --- Secret: Key Vault reference (resolved at runtime by the managed identity) ---
        { name: 'ConnectionStrings__Default', value: kvRef(keyVaultName, 'Db-Connection') }
        // --- Entra sign-in (ID-token-only → no client secret). The OIDC middleware derives
        //     the redirect from the request host + CallbackPath; that URL must also be listed
        //     in the admin app registration's redirectUris (entra/admin-app.bicep). ---
        { name: 'AzureAd__Instance', value: azureAdInstance }
        { name: 'AzureAd__TenantId', value: azureAdTenantId }
        { name: 'AzureAd__ClientId', value: azureAdClientId }
        { name: 'AzureAd__CallbackPath', value: '/signin-oidc' }
      ]
    }
  }
}

// Capture the admin host's stdout/console (default .NET logging) so it's viewable via
// az webapp log tail / Log Stream / Kudu, capped to 1 day / 25 MB so it rolls.
resource siteLogs 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: site
  name: 'logs'
  properties: {
    applicationLogs: {
      fileSystem: { level: 'Information' }
    }
    httpLogs: {
      fileSystem: {
        enabled: true
        retentionInDays: 1
        retentionInMb: 25
      }
    }
    detailedErrorMessages: { enabled: false }
    failedRequestsTracing: { enabled: false }
  }
}

@description('System-assigned managed identity principal id (for Key Vault access).')
output principalId string = site.identity.principalId

@description('Default hostname of the admin web app.')
output defaultHostname string = site.properties.defaultHostName
