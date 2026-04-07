param location string = 'eastus'

resource cognitiveAccount 'Microsoft.CognitiveServices/accounts@2022-10-01' = {
  name: 'washingtoncogsvc01'
  location: location
  kind: 'TextAnalytics'
  properties: {
    customSubDomainName: 'washingtoncogsvc01'
    publicNetworkAccess: 'Enabled'
  }
  sku: {
    name: 'S0'
    tier: 'Standard'
  }
}
