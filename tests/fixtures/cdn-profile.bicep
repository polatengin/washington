param location string = 'global'

resource cdnProfile 'Microsoft.Cdn/profiles@2024-02-01' = {
  name: 'washingtonfrontdoor01'
  location: location
  sku: {
    name: 'Standard_AzureFrontDoor'
  }
  properties: {}
}
