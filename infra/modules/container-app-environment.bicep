param name string
param location string
param tags object

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    zoneRedundant: false
  }
}

output id string = environment.id
output name string = environment.name
