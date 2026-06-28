// StuffInABox — Azure infrastructure (Bicep).
//
// Subscription-scoped: creates a resource group and deploys the app hosting into it.
// Secrets are NOT passed here — they live in Key Vault (named by `keyVaultName`) and the
// app reads them via its managed identity (Key Vault references). So the CI/CD pipeline
// handles NO secret values.
//
// One-time bootstrap (by an Owner, since it creates a role assignment): deploy with
// enableKeyVault=true to create the vault + grant the web app access, then add the secrets
// (Db-Connection, Jwt-Secret, Email-Smtp-Password, OAuth-Google-ClientSecret,
// OAuth-Microsoft-ClientSecret). After that, normal deploys run with enableKeyVault=false.
//
// Deploy:
//   az deployment sub create -l <location> -f infra/main.bicep -p infra/main.bicepparam
targetScope = 'subscription'

@description('Short environment name, used in resource names (e.g. dev, prod).')
param environmentName string = 'dev'

@description('Azure region.')
param location string = 'swedencentral'

@description('Globally-unique web app name (becomes <name>.azurewebsites.net).')
param appName string

@description('App Service plan SKU. F1 = Free (testing), B1 = Basic (~$13/mo).')
param appServiceSku string = 'F1'

@description('EF database provider for the app: "postgres" (Supabase/Azure) or "sqlite".')
param databaseProvider string = 'postgres'

@description('Name of the Key Vault that holds the app secrets.')
param keyVaultName string

// --- Non-secret app config (safe to pass as plain GitHub variables) ---
@description('Brevo SMTP login (an id, not a secret).')
param brevoSmtpUser string
@description('Verified sender address for outgoing email.')
param emailFrom string
@description('Google OAuth client id (public identifier).')
param googleClientId string = ''
@description('Microsoft OAuth client id (public identifier).')
param microsoftClientId string = ''

// --- Prepared modules. Key Vault is created/granted only on the one-time bootstrap
//     (needs Owner / User Access Administrator); normal pipeline deploys leave it false. ---
@description('Create the Key Vault + grant the web app access (one-time bootstrap by an Owner).')
param enableKeyVault bool = false
@description('Provision Azure Database for PostgreSQL Flexible Server (off = use Supabase).')
param enablePostgres bool = false
@description('Provision a Storage Account + blob container for photos (off = local disk).')
param enableStorage bool = false

@secure()
@description('Admin password if enablePostgres = true.')
param postgresAdminPassword string = ''

var rgName = 'rg-stuffinabox-${environmentName}'
var appBaseUrl = 'https://${appName}.azurewebsites.net'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: rgName
  location: location
}

module app 'modules/appService.bicep' = {
  scope: rg
  name: 'appService'
  params: {
    location: location
    appName: appName
    skuName: appServiceSku
    appBaseUrl: appBaseUrl
    databaseProvider: databaseProvider
    keyVaultName: keyVaultName
    brevoSmtpUser: brevoSmtpUser
    emailFrom: emailFrom
    googleClientId: googleClientId
    microsoftClientId: microsoftClientId
    storageProvider: enableStorage ? 'r2' : 'local'
  }
}

module keyVault 'modules/keyVault.bicep' = if (enableKeyVault) {
  scope: rg
  name: 'keyVault'
  params: {
    location: location
    vaultName: keyVaultName
    webAppPrincipalId: app.outputs.principalId
  }
}

module postgres 'modules/postgres.bicep' = if (enablePostgres) {
  scope: rg
  name: 'postgres'
  params: {
    location: location
    serverName: 'pg-stuffinabox-${environmentName}'
    adminPassword: postgresAdminPassword
  }
}

module storage 'modules/storage.bicep' = if (enableStorage) {
  scope: rg
  name: 'storage'
  params: {
    location: location
    // Storage account names: 3-24 chars, lowercase alphanumeric only.
    accountName: toLower(replace('sa${appName}${environmentName}', '-', ''))
  }
}

output webAppUrl string = appBaseUrl
output resourceGroup string = rgName
