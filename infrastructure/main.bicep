@description('The name of the environment (e.g., dev, staging, prod)')
param environmentName string

@description('The location for all resources')
param location string = resourceGroup().location

@description('The name suffix for resources')
param nameSuffix string = uniqueString(resourceGroup().id)

// Cosmos DB
module cosmos 'cosmos.bicep' = {
  name: 'cosmosDeployment'
  params: {
    cosmosAccountName: 'sendtokindle-${environmentName}-${nameSuffix}'
    location: location
  }
}

// Outputs
output cosmosEndpoint string = cosmos.outputs.cosmosEndpoint
output cosmosAccountName string = cosmos.outputs.cosmosAccountName
output databaseName string = cosmos.outputs.databaseName
