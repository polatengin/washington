param location string = 'eastus'

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: 'washingtonsqlsrv01'
  location: location
  properties: {
    administratorLogin: 'sqladminuser'
    administratorLoginPassword: 'Placeholder123!'
    version: '12.0'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: 'washingtonsqldb'
  sku: {
    name: 'S0'
    tier: 'Standard'
  }
}
