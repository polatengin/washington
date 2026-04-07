param location string = 'eastus'

resource appInsights 'Microsoft.Insights/components@2018-05-01-preview' = {
  name: 'washington-demo-ai'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}
