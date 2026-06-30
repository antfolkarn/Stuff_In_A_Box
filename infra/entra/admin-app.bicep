// Entra app registration for the ADMIN application (StuffInABox.Admin.Web) sign-in.
//
// Single-tenant: only accounts in the tenant this is deployed to can sign in, and every
// signed-in user is treated as an admin (the admin host's authorization is just
// "authenticated"). Uses the ID-token flow, so NO client secret is needed — we only enable
// ID token issuance below.
//
// Uses the Microsoft Graph Bicep extension (see ./bicepconfig.json): it deploys against
// Microsoft Graph at TENANT scope, so it needs a principal allowed to create app
// registrations (Application Administrator / Application.ReadWrite.All).
//
// SWITCHING TENANT (the whole point of having this as IaC): deploy this template into the
// new tenant, then update the admin host's AzureAd:TenantId (the new tenant id) and
// AzureAd:ClientId (this template's clientId output). Nothing else changes.
//
// Deploy (from infra/entra so the local bicepconfig.json applies):
//   az login --tenant <TENANT_ID> --allow-no-subscriptions
//   az deployment tenant create --location swedencentral --template-file admin-app.bicep \
//     --parameters redirectUris='["http://localhost:5180/signin-oidc"]'
//
// If tenant-scope deployment is denied on a personal tenant (AuthorizationFailed on the
// tenant root — known for the personal "Standardkatalog" tenant), use the CLI equivalent:
//   az ad app create --display-name "StuffInABox Admin" --sign-in-audience AzureADMyOrg \
//     --web-redirect-uris "http://localhost:5180/signin-oidc" --enable-id-token-issuance true

targetScope = 'tenant'

extension microsoftGraphV1

@description('Web redirect URIs (the /signin-oidc callback). Include localhost for dev and the deployed admin URL for prod.')
param redirectUris array = [
  'http://localhost:5180/signin-oidc'
]

@description('Display name of the app registration.')
param appDisplayName string = 'StuffInABox Admin'

@description('Stable identifier for the app registration within the tenant.')
param uniqueName string = 'stuffinabox-admin'

resource adminApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: uniqueName
  displayName: appDisplayName
  // Single tenant: this is the tenant lock that makes "authenticated == admin" safe.
  signInAudience: 'AzureADMyOrg'
  web: {
    redirectUris: redirectUris
    implicitGrantSettings: {
      // ID-token-only flow: lets the app authenticate without a client secret.
      enableIdTokenIssuance: true
      enableAccessTokenIssuance: false
    }
  }
}

@description('Application (client) id — use as the admin host AzureAd:ClientId.')
output clientId string = adminApp.appId
