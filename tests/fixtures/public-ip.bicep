param location string = 'eastus'

resource publicIp 'Microsoft.Network/publicIPAddresses@2020-06-01' = {
  name: 'washington-demo-pip'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAddressVersion: 'IPv4'
    publicIPAllocationMethod: 'Static'
    idleTimeoutInMinutes: 4
  }
}
