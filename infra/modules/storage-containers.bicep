param storageAccountName string

param mediaContainerName string = 'vibecast-media'
param knowledgeContainerName string = 'vibecast-knowledge'
param dataProtectionContainerName string = 'vibecast-dataprotection'

resource storageAccount 'Microsoft.Storage/storageAccounts@2025-01-01' existing = {
  name: storageAccountName
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2025-01-01' existing = {
  parent: storageAccount
  name: 'default'
}

resource mediaContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-01-01' = {
  parent: blobService
  name: mediaContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource knowledgeContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-01-01' existing = {
  parent: blobService
  name: knowledgeContainerName
}

resource dataProtectionContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-01-01' = {
  parent: blobService
  name: dataProtectionContainerName
  properties: {
    publicAccess: 'None'
  }
}

output storageAccountId string = storageAccount.id
output serviceUri string = 'https://${storageAccount.name}.blob.core.windows.net/'
output mediaContainerName string = mediaContainer.name
output knowledgeContainerName string = knowledgeContainer.name
output dataProtectionContainerName string = dataProtectionContainer.name
output dataProtectionBlobUri string = 'https://${storageAccount.name}.blob.core.windows.net/${dataProtectionContainer.name}/keys.xml'
