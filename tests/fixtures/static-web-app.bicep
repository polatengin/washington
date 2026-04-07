param location string = 'eastus2'

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: 'washingtonstaticapp01'
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}
