// PREPARED, OFF BY DEFAULT. Key Vault (RBAC mode) for holding the app's secrets, and a
// role assignment granting the web app's managed identity read access. To use it, move
// secrets into the vault and switch the web app's app settings to Key Vault references
// (@Microsoft.KeyVault(SecretUri=...)).

param location string

@description('Key Vault name: 3-24 chars, alphanumeric and dashes.')
param vaultName string

@description('Principal id of the web app managed identity to grant secret access.')
param webAppPrincipalId string

// Built-in role: Key Vault Secrets User (read secret values).
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: vaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

resource secretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: vault
  name: guid(vault.id, webAppPrincipalId, keyVaultSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output vaultUri string = vault.properties.vaultUri
