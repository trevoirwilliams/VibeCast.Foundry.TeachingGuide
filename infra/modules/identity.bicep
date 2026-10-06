param name string
param location string
param tags object

resource runtimeIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: name
  location: location
  tags: tags
}

output id string = runtimeIdentity.id
output clientId string = runtimeIdentity.properties.clientId
output principalId string = runtimeIdentity.properties.principalId
output name string = runtimeIdentity.name
