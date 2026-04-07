param location string = 'eastus'

resource eventGridTopic 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: 'washingtoneventgrid01'
  location: location
  properties: {
    inputSchema: 'EventGridSchema'
  }
}
