param location string = 'eastus'

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: 'washingtonsqlsrv02'
  location: location
  properties: {
    administratorLogin: 'sqladminuser'
    administratorLoginPassword: 'Placeholder123!'
    version: '12.0'
    publicNetworkAccess: 'Enabled'
  }
}
