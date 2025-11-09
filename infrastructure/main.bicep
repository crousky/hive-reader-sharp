@description('The name of the environment (e.g., dev, staging, prod)')
param environmentName string

@description('The location for all resources. Defaults to the resource group location (eastus2)')
param location string = resourceGroup().location

@description('The name suffix for resources')
param nameSuffix string = uniqueString(resourceGroup().id)

// Hive Reader Cosmos DB
module hiveReaderCosmos 'hive-reader-cosmos.bicep' = {
  name: 'hiveReaderCosmosDeployment'
  params: {
    cosmosAccountName: 'hive-reader-db-${environmentName}-${nameSuffix}'
    location: location
    databaseName: 'HiveReaderDB'
  }
}

// Static Web App
module staticWebApp 'static-web-app.bicep' = {
  name: 'staticWebAppDeployment'
  params: {
    staticWebAppName: 'hive-reader-web-${environmentName}'
    location: location
    sku: 'Free'
  }
}

// Legacy Cosmos DB (kept for backward compatibility)
module cosmos 'cosmos.bicep' = {
  name: 'cosmosDeployment'
  params: {
    cosmosAccountName: 'sendtokindle-${environmentName}-${nameSuffix}'
    location: location
  }
}

// Outputs for Hive Reader
output hiveReaderCosmosEndpoint string = hiveReaderCosmos.outputs.cosmosEndpoint
output hiveReaderCosmosAccountName string = hiveReaderCosmos.outputs.cosmosAccountName
output hiveReaderDatabaseName string = hiveReaderCosmos.outputs.databaseName
output hiveReaderCosmosConnectionString string = hiveReaderCosmos.outputs.cosmosConnectionString

// Static Web App Outputs
output staticWebAppId string = staticWebApp.outputs.staticWebAppId
output staticWebAppUrl string = 'https://${staticWebApp.outputs.staticWebAppDefaultHostname}'
output staticWebAppName string = staticWebApp.outputs.staticWebAppName
output staticWebAppDeploymentToken string = staticWebApp.outputs.deploymentToken

// Legacy Outputs (kept for backward compatibility)
output cosmosEndpoint string = cosmos.outputs.cosmosEndpoint
output cosmosAccountName string = cosmos.outputs.cosmosAccountName
output databaseName string = cosmos.outputs.databaseName
