param location string = 'eastus'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'washington-demo-plan'
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}