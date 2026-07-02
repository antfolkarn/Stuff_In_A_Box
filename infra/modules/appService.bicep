// App Service plan + Linux Web App that hosts the StuffInABox .NET app (which also
// serves the built SPA from wwwroot).
//
// Secrets are NOT passed to this template — they live in Key Vault and are wired in as
// Key Vault references (@Microsoft.KeyVault(...)). The web app's system-assigned managed
// identity reads them at runtime (it needs the "Key Vault Secrets User" role on the vault;
// see keyVault.bicep). So the deploy/pipeline never handles secret values.
//
// Prerequisite: the Key Vault named by `keyVaultName` must hold these secrets:
//   Db-Connection, Jwt-Secret, Email-Smtp-Password, OAuth-Google-ClientSecret,
//   OAuth-Microsoft-ClientSecret

@description('Azure region for the resources.')
param location string

@description('Globally-unique web app name (becomes <name>.azurewebsites.net).')
param appName string

@description('App Service plan SKU. F1 = Free (good for testing), B1 = Basic (~$13/mo), etc.')
param skuName string = 'F1'

@description('Linux runtime stack for the web app.')
param linuxFxVersion string = 'DOTNETCORE|10.0'

@description('Public base URL of the app — used for OAuth redirect + email links.')
param appBaseUrl string

@description('EF database provider: "postgres" (Supabase/Azure) or "sqlite".')
param databaseProvider string = 'postgres'

@description('Photo storage provider: "local", "r2" or "s3".')
param storageProvider string = 'local'

@description('Name of the Key Vault holding the app secrets.')
param keyVaultName string

@description('Brevo SMTP login (an id, not a secret).')
param brevoSmtpUser string

@description('Verified sender address for outgoing email.')
param emailFrom string

@description('Google OAuth client id (public identifier).')
param googleClientId string = ''

@description('Microsoft OAuth client id (public identifier).')
param microsoftClientId string = ''

@description('Image recognition provider: "ollama", "staik" or "none".')
param imageRecognitionProvider string = 'none'

@description('Base URL of the Ollama endpoint (e.g. a Tailscale Funnel address).')
param ollamaBaseUrl string = ''

@description('Ollama vision model name (e.g. gemma3:12b).')
param ollamaModel string = ''

@description('Request timeout (seconds) for Ollama recognition calls.')
param ollamaTimeoutSeconds int = 180

@description('Base URL of the Staik API.')
param staikBaseUrl string = 'https://api.staik.se'

@description('Staik vision model name (e.g. gemma4:31b).')
param staikModel string = 'gemma4:31b'

@description('Request timeout (seconds) for Staik recognition calls.')
param staikTimeoutSeconds int = 180

// Free/Shared tiers don't support Always On; Basic and up do.
var supportsAlwaysOn = !contains(['F1', 'FREE', 'D1', 'SHARED'], toUpper(skuName))

// Builds a Key Vault reference (latest version) for an app setting value.
func kvRef(vault string, secret string) string =>
  '@Microsoft.KeyVault(VaultName=${vault};SecretName=${secret})'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'plan-${appName}'
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
  name: appName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned' // used to read the Key Vault references below
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
        { name: 'App__BaseUrl', value: appBaseUrl }
        { name: 'Storage__Provider', value: storageProvider }
        // Write the Serilog file into the App Service log area so it shows up in the log
        // stream / Kudu. Retention + size are capped in code (Logging:File:*) to ~1 day.
        { name: 'Logging__File__Path', value: '/home/LogFiles/stuffinabox/app-.log' }
        // --- Secrets: Key Vault references (resolved at runtime by the managed identity) ---
        { name: 'ConnectionStrings__Default', value: kvRef(keyVaultName, 'Db-Connection') }
        { name: 'Jwt__Secret', value: kvRef(keyVaultName, 'Jwt-Secret') }
        { name: 'Email__Smtp__Password', value: kvRef(keyVaultName, 'Email-Smtp-Password') }
        { name: 'OAuth__Google__ClientSecret', value: kvRef(keyVaultName, 'OAuth-Google-ClientSecret') }
        { name: 'OAuth__Microsoft__ClientSecret', value: kvRef(keyVaultName, 'OAuth-Microsoft-ClientSecret') }
        // --- Non-secret config ---
        { name: 'Email__Provider', value: 'smtp' }
        { name: 'Email__Smtp__Host', value: 'smtp-relay.brevo.com' }
        { name: 'Email__Smtp__Port', value: '587' }
        { name: 'Email__Smtp__User', value: brevoSmtpUser }
        { name: 'Email__Smtp__From', value: emailFrom }
        { name: 'Email__Smtp__FromName', value: 'StuffInABox' }
        { name: 'OAuth__Google__ClientId', value: googleClientId }
        { name: 'OAuth__Google__RedirectUri', value: '${appBaseUrl}/api/v1/auth/google/callback' }
        { name: 'OAuth__Microsoft__ClientId', value: microsoftClientId }
        { name: 'OAuth__Microsoft__RedirectUri', value: '${appBaseUrl}/api/v1/auth/microsoft/callback' }
        // --- Image recognition. `Provider` selects which block below is used:
        //     "ollama" = self-hosted vision model reached over a tunnel;
        //     "staik"  = hosted OpenAI-compatible vision API (api.staik.se).
        //     Both API keys are Key Vault references; the unused provider's settings are harmless. ---
        { name: 'ImageRecognition__Provider', value: imageRecognitionProvider }
        { name: 'ImageRecognition__Ollama__BaseUrl', value: ollamaBaseUrl }
        { name: 'ImageRecognition__Ollama__Model', value: ollamaModel }
        { name: 'ImageRecognition__Ollama__TimeoutSeconds', value: string(ollamaTimeoutSeconds) }
        { name: 'ImageRecognition__Ollama__ApiKey', value: kvRef(keyVaultName, 'Ollama-ApiKey') }
        { name: 'ImageRecognition__Staik__BaseUrl', value: staikBaseUrl }
        { name: 'ImageRecognition__Staik__Model', value: staikModel }
        { name: 'ImageRecognition__Staik__TimeoutSeconds', value: string(staikTimeoutSeconds) }
        { name: 'ImageRecognition__Staik__ApiKey', value: kvRef(keyVaultName, 'Staik-ApiKey') }
      ]
    }
  }
}

// Enable App Service diagnostic logging so stdout/console is captured and viewable
// (az webapp log tail / Log Stream / Kudu). Retention is capped to 1 day / 25 MB so it
// rolls and can't fill the /home quota.
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

@description('Default hostname of the web app.')
output defaultHostname string = site.properties.defaultHostName
