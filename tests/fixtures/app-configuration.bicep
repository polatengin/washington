param location string = 'eastus'

resource appConfiguration 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: 'washingtonappconfig01'
  location: location
  sku: {
    name: 'Standard'
  }
}
