param location string = 'eastus'

resource purviewAccount 'Microsoft.Purview/accounts@2021-12-01' = {
  name: 'washingtonpurview01'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Standard'
    capacity: 1
  }
  properties: {
    managedResourceGroupName: 'washington-purview-managed'
  }
}
