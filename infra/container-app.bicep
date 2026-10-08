targetScope = 'resourceGroup'

@description('VibeCast Container App resource name.')
param appName string = 'ca-vibecast-prod'
@description('Location of the existing Container Apps environment.')
param location string
@description('Resource ID of the existing managed environment.')
param environmentId string
@description('Resource ID of id-vibecast-prod.')
param runtimeIdentityId string
@description('Client ID of id-vibecast-prod for ManagedIdentityCredential.')
param runtimeIdentityClientId string
@description('Login server for the existing ACR, without https://.')
param registryLoginServer string
@description('Versioned, fully qualified image reference.')
param image string
@description('PostgreSQL connection string; must contain no password.')
param postgresConnectionString string
@description('HTTPS Blob service URI.')
param storageServiceUri string
@description('Absolute URI of the shared Data Protection key-ring blob.')
param dataProtectionBlobUri string
@description('Azure Key Vault key identifier used to encrypt the key-ring.')
param dataProtectionKeyIdentifier string
@description('Microsoft Foundry project endpoint.')
param foundryProjectEndpoint string
@description('Previously deployed chat model deployment name.')
param foundryChatModelDeployment string
@description('Previously deployed image model deployment name.')
param foundryImageModelDeployment string
@description('Foundry Speech endpoint.')
param speechEndpoint string
@description('Content Understanding endpoint.')
param contentUnderstandingEndpoint string
@description('Existing Azure AI Search service endpoint.')
param knowledgeSearchEndpoint string

@description('Explicit temporary access to self-registration. Keep false for the published demo.')
param registrationEnabled bool = false
@description('Single temporary demo email permitted to register. No password; clear after onboarding.')
param registrationAllowedEmail string = ''
@minValue(0)
@maxValue(1)
@description('Minimum replicas. Zero permits cold starts and stops idle compute billing.')
param minReplicas int = 0

resource app 'Microsoft.App/containerApps@2025-01-01' = {
  name: appName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${runtimeIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: registryLoginServer
          identity: runtimeIdentityId
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        stickySessions: {
          affinity: 'sticky'
        }
      }
    }
    template: {
      containers: [
        {
          name: 'vibecast-web'
          image: image
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'ASPNETCORE_HTTP_PORTS', value: '8080' }
            { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
            { name: 'Registration__Enabled', value: string(registrationEnabled) }
            { name: 'Registration__AllowedEmail', value: registrationAllowedEmail }
            { name: 'ConnectionStrings__VibeCast', value: postgresConnectionString }
            { name: 'AzureIdentity__ManagedIdentityClientId', value: runtimeIdentityClientId }
            { name: 'MediaStorage__ServiceUri', value: storageServiceUri }
            { name: 'KnowledgeStorage__ServiceUri', value: storageServiceUri }
            { name: 'KnowledgeStorage__SearchEndpoint', value: knowledgeSearchEndpoint }
            { name: 'Foundry__ProjectEndpoint', value: foundryProjectEndpoint }
            { name: 'Foundry__ChatModelDeployment', value: foundryChatModelDeployment }
            { name: 'Foundry__ImageModelDeployment', value: foundryImageModelDeployment }
            { name: 'Speech__Endpoint', value: speechEndpoint }
            { name: 'ContentUnderstanding__Endpoint', value: contentUnderstandingEndpoint }
            { name: 'DataProtection__BlobUri', value: dataProtectionBlobUri }
            { name: 'DataProtection__KeyVaultKeyIdentifier', value: dataProtectionKeyIdentifier }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/alive'
                port: 8080
              }
              initialDelaySeconds: 2
              periodSeconds: 5
              timeoutSeconds: 3
              failureThreshold: 24
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/alive'
                port: 8080
              }
              initialDelaySeconds: 0
              periodSeconds: 10
              timeoutSeconds: 3
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 1
              periodSeconds: 5
              timeoutSeconds: 3
              failureThreshold: 12
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: 1
      }
    }
  }
}

output appId string = app.id
output ingressFqdn string = app.properties.configuration.ingress.fqdn
