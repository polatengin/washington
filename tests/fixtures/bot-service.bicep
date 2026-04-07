param resourceName string = 'washingtonbot01'

resource botService 'Microsoft.BotService/botServices@2021-05-01-preview' = {
  name: resourceName
  location: 'global'
  kind: 'sdk'
  properties: {
    displayName: resourceName
    endpoint: 'https://example.org/api/messages'
    msaAppId: '11111111-1111-1111-1111-111111111111'
  }
  sku: {
    name: 'F0'
  }
}
