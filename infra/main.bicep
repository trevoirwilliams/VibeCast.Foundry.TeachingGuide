targetScope = 'subscription'

param location string

param productionResourceGroupName string = 'rg-vibecast-prod'

param existingStorageSubscriptionId string = subscription().subscriptionId
param existingStorageResourceGroupName string
param existingStorageAccountName string = 'vibecastkb90423471'

var uniqueSuffix = uniqueString(
  subscription().id,
  productionResourceGroupName
)

var runtimeIdentityName = 'id-vibecast-prod'
var registryName = toLower('acrvibecast${uniqueSuffix}')
var keyVaultName = 'kv-vibecast-${take(uniqueSuffix, 12)}'
var postgresServerName = 'pg-vibecast-${uniqueSuffix}'
var containerAppsEnvironmentName = 'cae-vibecast-prod'

var commonTags = {
  application: 'VibeCast'
  environment: 'production'
  managedBy: 'Bicep'
  course: 'Generative AI for .NET'
}

resource productionResourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: productionResourceGroupName
  location: location
  tags: commonTags
}

module runtimeIdentity './modules/identity.bicep' = {
  name: 'runtime-identity'
  scope: productionResourceGroup
  params: {
    name: runtimeIdentityName
    location: location
    tags: commonTags
  }
}

module registry './modules/registry.bicep' = {
  name: 'container-registry'
  scope: productionResourceGroup
  params: {
    name: registryName
    location: location
    tags: commonTags
  }
}

module keyVault './modules/key-vault.bicep' = {
  name: 'key-vault'
  scope: productionResourceGroup
  params: {
    name: keyVaultName
    location: location
    tags: commonTags
  }
}

module postgres './modules/postgres.bicep' = {
  name: 'postgres'
  scope: productionResourceGroup
  params: {
    serverName: postgresServerName
    location: location
    tenantId: tenant().tenantId
    databaseName: 'vibecast'
    tags: commonTags
  }
}

module containerAppsEnvironment './modules/container-app-environment.bicep' = {
  name: 'container-app-environment'
  scope: productionResourceGroup
  params: {
    name: containerAppsEnvironmentName
    location: location
    tags: commonTags
  }
}

module storage './modules/storage-containers.bicep' = {
  name: 'storage-containers'
  scope: resourceGroup(
    existingStorageSubscriptionId,
    existingStorageResourceGroupName
  )
  params: {
    storageAccountName: existingStorageAccountName
    mediaContainerName: 'vibecast-media'
    knowledgeContainerName: 'vibecast-knowledge'
    dataProtectionContainerName: 'vibecast-dataprotection'
  }
}

output productionResourceGroupName string = productionResourceGroup.name

output runtimeIdentityId string = runtimeIdentity.outputs.id
output runtimeIdentityClientId string = runtimeIdentity.outputs.clientId
output runtimeIdentityPrincipalId string = runtimeIdentity.outputs.principalId

output registryId string = registry.outputs.id
output registryLoginServer string = registry.outputs.loginServer

output postgresServerId string = postgres.outputs.id
output postgresHost string = postgres.outputs.fullyQualifiedDomainName
output postgresDatabase string = postgres.outputs.databaseName
output postgresConnectionString string = postgres.outputs.connectionString

output containerAppsEnvironmentId string = containerAppsEnvironment.outputs.id

output keyVaultId string = keyVault.outputs.id
output keyVaultUri string = keyVault.outputs.vaultUri
output dataProtectionKeyIdentifier string = keyVault.outputs.keyIdentifier

output storageAccountId string = storage.outputs.storageAccountId
output storageServiceUri string = storage.outputs.serviceUri
output mediaContainerName string = storage.outputs.mediaContainerName
output knowledgeContainerName string = storage.outputs.knowledgeContainerName
output dataProtectionContainerName string = storage.outputs.dataProtectionContainerName
output dataProtectionBlobUri string = storage.outputs.dataProtectionBlobUri
