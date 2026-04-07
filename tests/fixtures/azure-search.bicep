param location string = 'eastus'

resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: 'washingtonsearch01'
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
  }
}
