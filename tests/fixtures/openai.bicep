param location string = 'eastus'

resource openAiAccount 'Microsoft.CognitiveServices/accounts@2022-10-01' = {
  name: 'washingtonopenai01'
  location: location
  kind: 'OpenAI'
  properties: {
    customSubDomainName: 'washingtonopenai01'
    publicNetworkAccess: 'Enabled'
  }
  sku: {
    name: 'S0'
    tier: 'Standard'
  }
}
