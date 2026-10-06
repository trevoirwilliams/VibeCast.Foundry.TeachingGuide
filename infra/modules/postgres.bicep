param serverName string
param location string
param tenantId string
param databaseName string = 'vibecast'

param entraAdminObjectId string
param entraAdminName string

@allowed([
  'User'
  'Group'
  'ServicePrincipal'
])
param entraAdminType string

param tags object

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: serverName
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    createMode: 'Default'
    version: '18'
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Disabled'
      tenantId: tenantId
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
    storage: {
      storageSizeGB: 32
      autoGrow: 'Enabled'
    }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: server
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

resource entraAdministrator 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
  parent: server
  name: entraAdminObjectId
  properties: {
    principalName: entraAdminName
    principalType: entraAdminType
    tenantId: tenantId
  }
}

resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: server
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output id string = server.id
output name string = server.name
output databaseName string = database.name
output fullyQualifiedDomainName string = server.properties.fullyQualifiedDomainName
output connectionString string = 'Host=${server.properties.fullyQualifiedDomainName};Port=5432;Database=${database.name};SSL Mode=Require'
