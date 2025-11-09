@description('The name of the Static Web App')
param staticWebAppName string

@description('The location for the Static Web App')
param location string = resourceGroup().location

@description('The SKU for the Static Web App')
@allowed([
  'Free'
  'Standard'
])
param sku string = 'Free'

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: sku
    tier: sku
  }
  properties: {
    repositoryUrl: ''
    branch: ''
    buildProperties: {
      appLocation: '/web'
      apiLocation: ''
      outputLocation: 'dist'
    }
  }
}

@description('The Static Web App resource ID')
output staticWebAppId string = staticWebApp.id

@description('The Static Web App default hostname')
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname

@description('The Static Web App name')
output staticWebAppName string = staticWebApp.name

@description('The deployment token for GitHub Actions')
output deploymentToken string = staticWebApp.listSecrets().properties.apiKey
