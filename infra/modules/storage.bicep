// PREPARED, OFF BY DEFAULT. Storage Account + private blob container for item photos —
// the Azure-native target if you move photos off local disk / R2. Note: the app would
// need an Azure Blob storage provider (it currently supports "local" and "r2"/"s3").

param location string

@description('Storage account name: 3-24 chars, lowercase alphanumeric only.')
param accountName string

param containerName string = 'photos'

resource account 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: accountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: account
  name: 'default'
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: containerName
  properties: {
    publicAccess: 'None'
  }
}

output accountName string = account.name
output blobEndpoint string = account.properties.primaryEndpoints.blob
