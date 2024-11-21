@description('The name of the App Service plan')
param appServicePlanName string = 'myAppServicePlan'

@description('The name of the Web App')
param webAppName string = 'myWebApp'

@description('The location for all resources')
param location string = resourceGroup().location

@description('The environment (e.g., dev, prod)')
param environment string = 'dev'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'F1' // Free tier for dev/test
    tier: 'Free'
  }
  properties: {
    reserved: false
  }
}

resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
  }
  tags: {
    environment: environment
  }
}
