using './main.bicep'

param location = readEnvironmentVariable('VIBECAST_AZURE_LOCATION')

param productionResourceGroupName = 'rg-vibecast-prod'

param existingStorageSubscriptionId = readEnvironmentVariable('VIBECAST_STORAGE_SUBSCRIPTION_ID')

param existingStorageResourceGroupName = readEnvironmentVariable('VIBECAST_STORAGE_RESOURCE_GROUP')

param existingStorageAccountName = 'vibecastkb90423471'
