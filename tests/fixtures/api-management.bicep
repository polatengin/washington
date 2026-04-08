param location string = 'eastus'

resource apiManagementService 'Microsoft.ApiManagement/service@2022-08-01' = {
  name: 'washingtonapim01'
  location: location
  sku: {
    name: 'Developer'
    capacity: 1
  }
  properties: {
    publisherName: 'Washington'
    publisherEmail: 'admin@example.com'
  }
}
