using './main.bicep'

param location =
  readEnvironmentVariable('VIBECAST_AZURE_LOCATION')

param productionResourceGroupName =
  'rg-vibecast-prod'

param existingStorageSubscriptionId =
  readEnvironmentVariable('VIBECAST_STORAGE_SUBSCRIPTION_ID')

param existingStorageResourceGroupName =
  readEnvironmentVariable('VIBECAST_STORAGE_RESOURCE_GROUP')

param existingStorageAccountName =
  'vibecastkb90423471'

param postgresEntraAdminObjectId =
  readEnvironmentVariable('VIBECAST_PG_ADMIN_OBJECT_ID')

param postgresEntraAdminName =
  readEnvironmentVariable('VIBECAST_PG_ADMIN_NAME')

param postgresEntraAdminType =
  'User'
