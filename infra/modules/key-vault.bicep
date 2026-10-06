param name string
param location string
param tags object
param keyName string = 'data-protection'

resource vault 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    publicNetworkAccess: 'Enabled'
    sku: {
      family: 'A'
      name: 'standard'
    }
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

resource dataProtectionKey 'Microsoft.KeyVault/vaults/keys@2024-11-01' = {
  parent: vault
  name: keyName
  properties: {
    kty: 'RSA'
    keySize: 2048
    keyOps: [
      'wrapKey'
      'unwrapKey'
    ]
  }
}

output id string = vault.id
output name string = vault.name
output vaultUri string = 'https://${vault.name}.vault.azure.net/'
output keyIdentifier string = 'https://${vault.name}.vault.azure.net/keys/${dataProtectionKey.name}'
