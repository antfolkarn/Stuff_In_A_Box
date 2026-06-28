// Entra app registration used for "Sign in with Microsoft" (the app's OAuth provider).
//
// Uses the Microsoft Graph Bicep extension — this deploys against Microsoft Graph, NOT
// ARM, so it's deployed on its own (tenant scope) and needs a principal allowed to create
// app registrations (Application Administrator / Application.ReadWrite.All).
//
// IMPORTANT: the client SECRET is generated server-side and cannot be emitted by Bicep.
// After this creates the app, mint a secret once and store it as the app setting
// OAuth:Microsoft:ClientSecret (and the GitHub secret SIB_MS_CLIENT_SECRET):
//   az ad app credential reset --id <clientId> --display-name stuffinabox --query password -o tsv
//
// Deploy (from infra/entra so the local bicepconfig.json applies):
//   az deployment tenant create --location swedencentral --template-file signin-app.bicep \
//     --parameters redirectUri=https://stuffinabox-andree.azurewebsites.net/api/v1/auth/microsoft/callback

targetScope = 'tenant'

extension microsoftGraphV1

@description('Public OAuth callback URL of the app.')
param redirectUri string

@description('Display name of the app registration.')
param appDisplayName string = 'StuffInABox Sign-in'

@description('Stable identifier for the app registration within the tenant.')
param uniqueName string = 'stuffinabox-signin'

resource signInApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: uniqueName
  displayName: appDisplayName
  // Both personal Microsoft accounts (outlook/hotmail) and work/school (Entra) accounts.
  signInAudience: 'AzureADandPersonalMicrosoftAccount'
  web: {
    redirectUris: [
      redirectUri
    ]
    implicitGrantSettings: {
      enableIdTokenIssuance: false
      enableAccessTokenIssuance: false
    }
  }
}

@description('Application (client) id — use as OAuth:Microsoft:ClientId.')
output clientId string = signInApp.appId
