// PREPARED, OFF BY DEFAULT. Azure Database for PostgreSQL Flexible Server — the
// migration target if you move off Supabase. Burstable B1ms is the cheapest tier and
// is free for 12 months on a new Azure free account. After provisioning, point the app
// at it by setting ConnectionStrings__Default (a Npgsql string) on the web app.

param location string
param serverName string

@description('Burstable is the cheapest tier; B1ms is free for 12 months on a free account.')
param skuName string = 'Standard_B1ms'
param storageSizeGB int = 32
param postgresVersion string = '16'
param adminUsername string = 'sibadmin'

@secure()
param adminPassword string

@description('Database created on the server.')
param databaseName string = 'stuffinabox'

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: serverName
  location: location
  sku: {
    name: skuName
    tier: 'Burstable'
  }
  properties: {
    version: postgresVersion
    administratorLogin: adminUsername
    administratorLoginPassword: adminPassword
    storage: {
      storageSizeGB: storageSizeGB
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: server
  name: databaseName
}

// Allow other Azure services (e.g. the App Service) to connect.
resource allowAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: server
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output fullyQualifiedDomainName string = server.properties.fullyQualifiedDomainName
@description('Template connection string — fill in the password at deploy/runtime.')
output connectionStringTemplate string = 'Host=${server.properties.fullyQualifiedDomainName};Database=${databaseName};Username=${adminUsername};Password=<secret>;SSL Mode=VerifyFull'
