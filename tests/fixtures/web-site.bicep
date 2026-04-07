param location string = 'eastus'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'washingtonwebplan'
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}

resource webSite 'Microsoft.Web/sites@2023-12-01' = {
  name: 'washingtonwebsite01'
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
  }
}
