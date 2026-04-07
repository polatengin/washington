param location string = 'eastus'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2019-12-01-preview' = {
  name: 'washingtondemoregistry01'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}
